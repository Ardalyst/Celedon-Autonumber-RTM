// Author: Matt Barnes (matt.barnes@celedonpartners.com)
/*The MIT License (MIT)

Copyright (c) 2015 Celedon Partners 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// Gets the next available number and adds it to the Target

using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
	public class getNextAutoNumber : CeledonPlugin
	{
		//
		// This is the main plugin that creates the numbers and adds them to new records
		// This plugin is not registered by default.  It is registered and unregistered dynamically by the CreateAutoNumber and DeleteAutoNumber plugins respectively
		//

		private AutoNumberPluginConfig config;

		public getNextAutoNumber(string pluginConfig, string secureConfig)
		{
			// Need to support older version
			if (pluginConfig.TryParseJSON<AutoNumberPluginConfig>(out config))
			{
					RegisterEvent(PREOPERATION, config.EventName, config.EntityName, Execute);
			}
			else
			{
				RegisterEvent(PREOPERATION, CREATEMESSAGE, pluginConfig, Execute);
			}

			
		}

		protected void Execute(LocalPluginContext context)
		{
			#region Get the list of autonumber records applicable to the Target entity type
			int triggerEvent = context.PluginExecutionContext.MessageName == "Update" ? 1 : 0;
			var autoNumberIdList = context.OrganizationDataContext.CreateQuery("cel_autonumber")
																  .Where(a => a.GetAttributeValue<string>("cel_entityname").Equals(context.PluginExecutionContext.PrimaryEntityName) && a.GetAttributeValue<OptionSetValue>("statecode").Value == 0 && a.GetAttributeValue<OptionSetValue>("cel_triggerevent").Value == triggerEvent)
																  .OrderBy(a => a.GetAttributeValue<Guid>("cel_autonumberid"))  // Insure they are ordered, to prevent deadlocks
																  .Select(a => a.GetAttributeValue<Guid>("cel_autonumberid"));
			#endregion

			#region This loop locks the autonumber record(s) so only THIS transaction can read/write it
			foreach (Guid autoNumberId in autoNumberIdList)
			{
				Entity lockingUpdate = new Entity("cel_autonumber");
				lockingUpdate.Id = autoNumberId;
				lockingUpdate["cel_preview"] = "555";  // Use the preview field as our "dummy" field - so we don't need a dedicated "dummy"

				context.OrganizationService.Update(lockingUpdate);
			}
			#endregion

			#region This loop populates the Target record, and updates the autonumber record(s)
			Entity Target = context.PluginExecutionContext.InputParameters["Target"] as Entity;

			foreach (Guid autoNumberId in autoNumberIdList)
			{
				Entity autoNumber = context.OrganizationService.Retrieve("cel_autonumber", autoNumberId, true);
				string targetAttribute = autoNumber.GetAttributeValue<string>("cel_attributename");

				#region Check conditions that prevent creating an autonumber
				if (context.PluginExecutionContext.MessageName == "Update" && !Target.Contains(autoNumber.GetAttributeValue<string>("cel_triggerattribute")))
				{
					continue;  // Continue, if this is an Update event and the Target does not contain the trigger value
				}
				else if ((autoNumber.Contains("cel_conditionaloptionset") && (!Target.Contains(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")) || Target.GetAttributeValue<OptionSetValue>(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")).Value != autoNumber.GetAttributeValue<int>("cel_conditionalvalue"))))
				{
					continue;  // Continue, if this is a conditional optionset
				}
				else if (Target.Contains(targetAttribute) && !String.IsNullOrWhiteSpace(Target.GetAttributeValue<string>(targetAttribute)))
				{
					continue;  // Continue, so we don't overwrite an existing value
				}
				#endregion

				#region Create the AutoNumber
				int numDigits = autoNumber.GetAttributeValue<int>("cel_digits");

				// Generate number and insert into Target Record
				Target[targetAttribute] = String.Format("{0}{1}{2}", ReplaceParameters(autoNumber.GetAttributeValue<string>("cel_prefix"), Target, context.OrganizationService),
																	 numDigits == 0 ? "" : autoNumber.GetAttributeValue<int>("cel_nextnumber").ToString("D" + numDigits),
																	 ReplaceParameters(autoNumber.GetAttributeValue<string>("cel_suffix"), Target, context.OrganizationService));

				// Increment next number in db
				Entity updatedAutoNumber = new Entity("cel_autonumber");
				updatedAutoNumber.Id = autoNumber.Id;
				updatedAutoNumber["cel_nextnumber"] = autoNumber.GetAttributeValue<int>("cel_nextnumber") + 1;
				updatedAutoNumber["cel_preview"] = Target[targetAttribute];  // fix the preview

				context.OrganizationService.Update(updatedAutoNumber);
				#endregion
			}
			#endregion
		}

		#region Process Runtime Parameters, if any
		private string ReplaceParameters(string text, Entity Target, IOrganizationService Service)
		{
			if (String.IsNullOrWhiteSpace(text))
			{
				return "";
			}
			
			foreach (RuntimeParameter param in RuntimeParameter.GetParametersFromString(text))
			{
				if (!param.IsParentParameter())
				{
					text = text.Replace(param.ParameterText, param.GetParameterValue(Target));
				}
				else 
				{
					if (Target.Contains(param.ParentLookupName))
					{
						var parentRecord = Service.Retrieve(Target.GetAttributeValue<EntityReference>(param.ParentLookupName).LogicalName, Target.GetAttributeValue<EntityReference>(param.ParentLookupName).Id, new ColumnSet(param.AttributeName));
						text = text.Replace(param.ParameterText, param.GetParameterValue(parentRecord));
					}
					else  // Target record has no parent, so use default value
					{
						text = text.Replace(param.ParameterText, param.DefaultValue);
					}
				}
			}

			return text;
		}
		#endregion
	}
}
