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
using Celedon.Constants;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
	public class GetNextAutoNumber : CeledonPlugin
	{
		//
		// This is the main plugin that creates the numbers and adds them to new records
		// This plugin is not registered by default.  It is registered and unregistered dynamically by the CreateAutoNumber and DeleteAutoNumber plugins respectively
		//

		public GetNextAutoNumber(string pluginConfig)
		{
		    // Need to support older version
            if (pluginConfig.TryParseJson(out AutoNumberPluginConfig config))
			{
					RegisterEvent(PipelineStage.PreOperation, config.EventName, config.EntityName, Execute);
			}
			else
			{
				RegisterEvent(PipelineStage.PreOperation, PipelineMessage.Create, pluginConfig, Execute);
			}
		}

		protected void Execute(LocalPluginContext context)
		{
			#region Get the list of autonumber records applicable to the target entity type
			int triggerEvent = context.PluginExecutionContext.MessageName == "Update" ? 1 : 0;
			var autoNumberIdList = context.OrganizationDataContext.CreateQuery("cel_autonumber")
																  .Where(a => a.GetAttributeValue<string>("cel_entityname").Equals(context.PluginExecutionContext.PrimaryEntityName) && a.GetAttributeValue<OptionSetValue>("statecode").Value == 0 && a.GetAttributeValue<OptionSetValue>("cel_triggerevent").Value == triggerEvent)
																  .OrderBy(a => a.GetAttributeValue<Guid>("cel_autonumberid"))  // Insure they are ordered, to prevent deadlocks
																  .Select(a => a.GetAttributeValue<Guid>("cel_autonumberid"));
			#endregion

			#region This loop locks the autonumber record(s) so only THIS transaction can read/write it
			foreach (var autoNumberId in autoNumberIdList)
			{
			    var lockingUpdate = new Entity("cel_autonumber")
			    {
			        Id = autoNumberId,
			        ["cel_preview"] = "555"
			    };
			    // Use the preview field as our "dummy" field - so we don't need a dedicated "dummy"

			    context.OrganizationService.Update(lockingUpdate);
			}
			#endregion

			#region This loop populates the target record, and updates the autonumber record(s)

		    if (!(context.PluginExecutionContext.InputParameters["target"] is Entity target))
		    {
		        return;
		    }

			foreach (var autoNumberId in autoNumberIdList)
			{
				var autoNumber = context.OrganizationService.Retrieve("cel_autonumber", autoNumberId, new ColumnSet(
                    "cel_attributename", 
                    "cel_triggerattribute", 
                    "cel_conditionaloptionset", 
                    "cel_conditionalvalue",
                    "cel_digits",
                    "cel_prefix",
                    "cel_nextnumber",
                    "cel_suffix"));

				var targetAttribute = autoNumber.GetAttributeValue<string>("cel_attributename");

				#region Check conditions that prevent creating an autonumber
				if (context.PluginExecutionContext.MessageName == "Update" && !target.Contains(autoNumber.GetAttributeValue<string>("cel_triggerattribute")))
				{
					continue;  // Continue, if this is an Update event and the target does not contain the trigger value
				}

                if ((autoNumber.Contains("cel_conditionaloptionset") && (!target.Contains(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")) || target.GetAttributeValue<OptionSetValue>(autoNumber.GetAttributeValue<string>("cel_conditionaloptionset")).Value != autoNumber.GetAttributeValue<int>("cel_conditionalvalue"))))
				{
					continue;  // Continue, if this is a conditional optionset
				}

                if (target.Contains(targetAttribute) && !string.IsNullOrWhiteSpace(target.GetAttributeValue<string>(targetAttribute)))
				{
					continue;  // Continue, so we don't overwrite an existing value
				}
				#endregion

				#region Create the AutoNumber
				var numDigits = autoNumber.GetAttributeValue<int>("cel_digits");

			    var prefix = context.OrganizationService.ReplaceParameters(target, autoNumber.GetAttributeValue<string>("cel_prefix"));

			    var number = numDigits == 0 ? "" : autoNumber.GetAttributeValue<int>("cel_nextnumber").ToString("D" + numDigits);

                var postfix = context.OrganizationService.ReplaceParameters(target, autoNumber.GetAttributeValue<string>("cel_suffix"));
                // Generate number and insert into target Record
			    target[targetAttribute] = $"{prefix}{number}{postfix}";

				// Increment next number in db
			    var updatedAutoNumber = new Entity("cel_autonumber")
			    {
			        Id = autoNumber.Id,
			        ["cel_nextnumber"] = autoNumber.GetAttributeValue<int>("cel_nextnumber") + 1,
			        ["cel_preview"] = target[targetAttribute]
			    };

			    context.OrganizationService.Update(updatedAutoNumber);
				#endregion
			}
			#endregion
		}
	}
}
