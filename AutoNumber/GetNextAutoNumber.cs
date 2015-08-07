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

namespace Celedon
{
	public class getNextAutoNumber : CeledonPlugin
	{
		//
		// This is the main plugin that creates the numbers and adds them to new records
		// This plugin is not registered by default.  It is registered and unregistered dynamically by the CreateAutoNumber and DeleteAutoNumber plugins respectively
		//
		public getNextAutoNumber(string entityName, string secureConfig)
		{
			RegisterEvent(PREOPERATION, CREATEMESSAGE, entityName, Execute);
		}

		protected void Execute(LocalPluginContext context)
		{
			#region Get the list of autonumber records applicable to the Target entity type
			var autoNumberIdList = context.OrganizationDataContext.CreateQuery("cel_autonumber")
																  .Where(a => a.GetAttributeValue<string>("cel_entityname").Equals(context.PluginExecutionContext.PrimaryEntityName) && a.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
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
			Entity Target = context.GetInputParameters<CreateInputParameters>().Target;

			foreach (Guid autoNumberId in autoNumberIdList)
			{
				Entity autoNumber = context.OrganizationService.Retrieve("cel_autonumber", autoNumberId, true);

				if ((autoNumber.Contains("cel_conditionaloptionset") && (!Target.Contains(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")) || Target.GetAttributeValue<OptionSetValue>(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")).Value != autoNumber.GetAttributeValue<int>("cel_conditionalvalue"))))
				{
					continue;  // Continue, if this is a conditional optionset
				}
				//else if (autoNumber.Contains("cel_conditionallookup") && (!Target.Contains(autoNumber.GetAttributeValue<string>("cel_conditionallookup")) || Target.GetAttributeValue<EntityReference>(autoNumber.GetAttributeValue<string>("cel_conditionallookup")).Id != Guid.Parse(autoNumber.GetAttributeValue<string>("cel_conditionallookupvalue"))))
				//{
				//	continue;  // Continue, if this is a conditional lookup
				//}
				else if (Target.Contains(autoNumber.GetAttributeValue<string>("cel_attributename")))
				{
					continue;  // Continue, so we don't overwrite an existing value
				}

				// Generate number and insert into Target Record
				Target[autoNumber.GetAttributeValue<string>("cel_attributename")] = String.Format("{0}{1}{2}", ReplaceParameters(autoNumber.GetAttributeValue<string>("cel_prefix"), Target, context.OrganizationService),
																											   autoNumber.GetAttributeValue<int>("cel_nextnumber").ToString("D" + autoNumber.GetAttributeValue<int>("cel_digits")),
																											   ReplaceParameters(autoNumber.GetAttributeValue<string>("cel_suffix"), Target, context.OrganizationService));

				// Increment next number in db
				Entity updatedAutoNumber = new Entity("cel_autonumber");
				updatedAutoNumber.Id = autoNumber.Id;
				updatedAutoNumber["cel_nextnumber"] = autoNumber.GetAttributeValue<int>("cel_nextnumber") + 1;
				updatedAutoNumber["cel_preview"] = Target[autoNumber.GetAttributeValue<string>("cel_attributename")];  // fix the preview

				context.OrganizationService.Update(updatedAutoNumber);
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
						var parentRecord = Service.Retrieve(Target.GetAttributeValue<EntityReference>(param.ParentLookupName).LogicalName, Target.GetAttributeValue<EntityReference>(param.ParentLookupName).Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(param.AttributeName));
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
