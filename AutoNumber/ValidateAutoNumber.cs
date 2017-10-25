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

// Validation on pre-create of a new AutoNumber record

#define VALIDATEPARAMETERS  // temporarily disable this, until it can be made to work
#define DUPLICATECHECK

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text.RegularExpressions;
using Celedon.Constants;

namespace Celedon
{
	public class ValidateAutoNumber : CeledonPlugin
	{
        //
        // This Plugin will validate the details of a new AutoNumber record before it is created
        //
        // Registration Details:
        // Message: Create
        // Primary Entity: cel_autonumber
        // User Context: SYSTEM
        // Event Pipeline: PreValidation
        // Mode: Sync
        // Config: none
        //

        // private LocalPluginContext Context;
        // private Dictionary<string, List<AttributeMetadata>> _entityMetadata;
        // private Dictionary<string, AttributeMetadata> AttributeMetadata;

	    // private readonly ConcurrentDictionary<string, AttributeMetadata> _attributeMetadata = new ConcurrentDictionary<string, AttributeMetadata>();
	    // private readonly ConcurrentDictionary<string, List<AttributeMetadata>> _entityMetadata = new ConcurrentDictionary<string, List<AttributeMetadata>>();

        public ValidateAutoNumber()
		{
			RegisterEvent(PipelineStage.PreValidation, PipelineMessage.Create, "cel_autonumber", Execute);
		}

		protected void Execute(LocalPluginContext context)
		{
			context.Trace("Getting Target entity");
			var target = context.GetInputParameters<CreateInputParameters>().Target;
		    context.Trace("Validate the Entity name");
		    context.Trace("Get Attribute List");

			var attributeList = GetEntityMetadata(context, target.GetAttributeValue<string>("cel_entityname"));

		    context.Trace("Validate the Attribute name");
			if (!attributeList.Select(a => a.LogicalName).Contains(target.GetAttributeValue<string>("cel_attributename")))
			{
				throw new InvalidPluginExecutionException("Specified Attribute does not exist.");
			}

		    context.Trace("Validate the Trigger Attribute (if any)");
			if (!string.IsNullOrEmpty(target.GetAttributeValue<string>("cel_triggerattribute")) && !attributeList.Select(a => a.LogicalName).Contains(target.GetAttributeValue<string>("cel_triggerattribute")))
			{
				throw new InvalidPluginExecutionException("Specified Trigger Attribute does not exist.");
			}

		    context.Trace("Validate the Attribute type");
			if (attributeList.Single(a => a.LogicalName.Equals(target.GetAttributeValue<string>("cel_attributename"))).AttributeType != AttributeTypeCode.String && attributeList.Single(a => a.LogicalName.Equals(target.GetAttributeValue<string>("cel_attributename"))).AttributeType != AttributeTypeCode.Memo)
			{
				throw new InvalidPluginExecutionException("Attribute must be a text field.");
			}

			#region test parameters
#if VALIDATEPARAMETERS
			var fields = new Dictionary<string, string>() { { "cel_prefix", "Prefix" }, { "cel_suffix", "Suffix" } };

			foreach (var field in fields.Keys)
			{
			    if (!target.Contains(field) || !target.GetAttributeValue<string>(field).Contains('{'))
			    {
			        continue;
			    }

			    if (target.GetAttributeValue<string>(field).Count(c => c.Equals('{')) != target.GetAttributeValue<string>(field).Count(c => c.Equals('}')))
			    {
			        throw new InvalidPluginExecutionException($"Invalid parameter formatting in {fields[field]}");
			    }

			    if (Regex.Matches(target.GetAttributeValue<string>(field), @"{(.*?)}").OfType<Match>().Select(m => m.Groups[0].Value).Distinct().Any(p => p.Substring(1).Contains('{')))
			    {
			        throw new InvalidPluginExecutionException($"Invalid parameter formatting in {fields[field]}");
			    }

			    try
			    {
			        foreach (var param in RuntimeParameter.GetParametersFromString(target.GetAttributeValue<string>(field)))
			        {
			            if (!param.IsParentParameter())
			            {
			                if (!attributeList.Select(a => a.LogicalName).Contains(param.AttributeName))
			                {
			                    throw new InvalidPluginExecutionException($"{param.AttributeName} is not a valid attribute name in {fields[field]} value");
			                }
			            }
			            else
			            {
			                if (!attributeList.Select(a => a.LogicalName).Contains(param.ParentLookupName))
			                {
			                    throw new InvalidPluginExecutionException($"{param.ParentLookupName} is not a valid attribute name in {fields[field]} value");
			                }

			                if (attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Lookup && attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Customer && attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Owner)
			                {
			                    throw new InvalidPluginExecutionException($"{param.ParentLookupName} must be a Lookup attribute type in {fields[field]} value");
			                }

			                var parentLookupAttribute = (LookupAttributeMetadata)GetAttributeMetadata(context, target.GetAttributeValue<string>("cel_entityname"), param.ParentLookupName);

			                if (!parentLookupAttribute.Targets.Any(e => GetEntityMetadata(context, e).Select(a => a.LogicalName).Contains(param.AttributeName)))
			                {
			                    throw new InvalidPluginExecutionException($"Invalid attribute on {param.ParentLookupName} parent entity, in {fields[field]} value");
			                }
			            }
			        }
			    }
			    catch (InvalidPluginExecutionException)
			    {
			        throw;
			    }
			    catch
			    {
			        throw new InvalidPluginExecutionException($"Failed to parse Runtime Parameters in {fields[field]} value.");
			    }
			}
#endif
			#endregion

			if (target.Contains("cel_conditionaloptionset"))
			{
			    context.Trace("Validate Conditional OptionSet");
				if (!attributeList.Select(a => a.LogicalName).Contains(target.GetAttributeValue<string>("cel_conditionaloptionset")))
				{
					throw new InvalidPluginExecutionException("Specified Conditional OptionSet does not exist");
				}

				if (attributeList.Single(a => a.LogicalName.Equals(target.GetAttributeValue<string>("cel_conditionaloptionset"))).AttributeType != AttributeTypeCode.Picklist)
				{
					throw new InvalidPluginExecutionException("Conditional Attribute must be an OptionSet");
				}

			    context.Trace("Validate Conditional Value");
				var optionSetMetadata = (PicklistAttributeMetadata)GetAttributeMetadata(context, target.GetAttributeValue<string>("cel_entityname"), target.GetAttributeValue<string>("cel_conditionaloptionset"));//attributeResponse.AttributeMetadata;
				if (!optionSetMetadata.OptionSet.Options.Select(o => o.Value).Contains(target.GetAttributeValue<int>("cel_conditionalvalue")))
				{
					throw new InvalidPluginExecutionException("Conditional Value does not exist in OptionSet");
				}
			}

			#region Duplicate Check
#if DUPLICATECHECK
			context.Trace("Validate there are no duplicates");
			// TODO: Fix this. duplicate detection works when all fields contain data, but fails when some fields are empty
			var autoNumberList = context.OrganizationDataContext.CreateQuery("cel_autonumber")
																.Where(a => a.GetAttributeValue<string>("cel_entityname").Equals(target.GetAttributeValue<string>("cel_entityname")) && a.GetAttributeValue<string>("cel_attributename").Equals(target.GetAttributeValue<string>("cel_attributename")))
																.Select(a => new { Id = a.GetAttributeValue<Guid>("cel_autonumberid"), ConditionalOption = a.GetAttributeValue<string>("cel_conditionaloptionset"), ConditionalValue = a.GetAttributeValue<int>("cel_conditionalvalue") })
																.ToList();


			if (!target.Contains("cel_conditionaloptionset") && autoNumberList.Any())
			{
				throw new InvalidPluginExecutionException("Duplicate AutoNumber record exists.");
			}

            if (autoNumberList.Any(a => a.ConditionalOption.Equals(target.GetAttributeValue<string>("cel_conditionaloptionset")) && a.ConditionalValue.Equals(target.GetAttributeValue<int>("cel_conditionalvalue"))))
			{
				throw new InvalidPluginExecutionException("Duplicate AutoNumber record exists.");
			}
#endif
            #endregion

		    context.Trace("Insert the autoNumber Name attribute");
			target["cel_name"] = $"AutoNumber for {target.GetAttributeValue<string>("cel_entityname")}, {target.GetAttributeValue<string>("cel_attributename")}";
		}

		private static AttributeMetadata GetAttributeMetadata(LocalPluginContext context, string entityName, string attributeName)
		{
		    try
		    {
		        var response = (RetrieveAttributeResponse)context.OrganizationService.Execute(new RetrieveAttributeRequest() { EntityLogicalName = entityName, LogicalName = attributeName });
		        return response.AttributeMetadata;
		    }
		    catch
		    {
		        throw new InvalidPluginExecutionException($"{attributeName} attribute does not exist on {entityName} entity, or entity does not exist.");
		    }
		}

		private static List<AttributeMetadata> GetEntityMetadata(LocalPluginContext context, string entityName)
		{
		    try
		    {
		        var response = (RetrieveEntityResponse)context.OrganizationDataContext.Execute(new RetrieveEntityRequest() { EntityFilters = EntityFilters.Attributes, LogicalName = entityName });
		        return response.EntityMetadata.Attributes.ToList(); 
		    }
		    catch
		    {
		        throw new InvalidPluginExecutionException($"{entityName} Entity does not exist.");
		    }
        }
	}
}
