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
		private LocalPluginContext Context;
		private Dictionary<string, List<AttributeMetadata>> EntityMetadata;
		private Dictionary<string, AttributeMetadata> AttributeMetadata;

		public ValidateAutoNumber(string unsecureConfig, string secureConfig)
		{
			EntityMetadata = new Dictionary<string, List<AttributeMetadata>>();
			AttributeMetadata = new Dictionary<string, AttributeMetadata>();

			RegisterEvent(PREVALIDATION, CREATEMESSAGE, "cel_autonumber", Execute);
		}

		protected void Execute(LocalPluginContext context)
		{
			Context = context;

			Trace("Getting Target entity");
			Entity Target = Context.GetInputParameters<CreateInputParameters>().Target;
			Trace("Validate the Entity name");
			Trace("Get Attribute List");
			List<AttributeMetadata> attributeList = GetEntityMetadata(Target.GetAttributeValue<string>("cel_entityname"));

			Trace("Validate the Attribute name");
			if (!attributeList.Select(a => a.LogicalName).Contains(Target.GetAttributeValue<string>("cel_attributename")))
			{
				throw new InvalidPluginExecutionException("Specified Attribute does not exist.");
			}

			Trace("Validate the Trigger Attribute (if any)");
			if (!String.IsNullOrEmpty(Target.GetAttributeValue<string>("cel_triggerattribute")) && !attributeList.Select(a => a.LogicalName).Contains(Target.GetAttributeValue<string>("cel_triggerattribute")))
			{
				throw new InvalidPluginExecutionException("Specified Trigger Attribute does not exist.");
			}

			Trace("Validate the Attribute type");
			if (attributeList.Single(a => a.LogicalName.Equals(Target.GetAttributeValue<string>("cel_attributename"))).AttributeType != AttributeTypeCode.String && attributeList.Single(a => a.LogicalName.Equals(Target.GetAttributeValue<string>("cel_attributename"))).AttributeType != AttributeTypeCode.Memo)
			{
				throw new InvalidPluginExecutionException("Attribute must be a text field.");
			}

			#region test parameters
#if VALIDATEPARAMETERS
			Dictionary<string, string> fields = new Dictionary<string, string>() { { "cel_prefix", "Prefix" }, { "cel_suffix", "Suffix" } };

			foreach (string field in fields.Keys)
			{
				if (Target.Contains(field) && Target.GetAttributeValue<string>(field).Contains('{'))
				{
					if (Target.GetAttributeValue<string>(field).Count(c => c.Equals('{')) != Target.GetAttributeValue<string>(field).Count(c => c.Equals('}')))
					{
						throw new InvalidPluginExecutionException(String.Format("Invalid parameter formatting in {0}", fields[field]));
					}

					foreach (string p in Regex.Matches(Target.GetAttributeValue<string>(field), @"{(.*?)}").OfType<Match>().Select(m => m.Groups[0].Value).Distinct())
					{
						if (p.Substring(1).Contains('{'))
						{
							throw new InvalidPluginExecutionException(String.Format("Invalid parameter formatting in {0}", fields[field]));
						}
					}

					try
					{
						foreach (RuntimeParameter param in RuntimeParameter.GetParametersFromString(Target.GetAttributeValue<string>(field)))
						{
							if (!param.IsParentParameter())
							{
								if (!attributeList.Select(a => a.LogicalName).Contains(param.AttributeName))
								{
									throw new InvalidPluginExecutionException(String.Format("{0} is not a valid attribute name in {1} value", param.AttributeName, fields[field]));
								}
							}
							else
							{
								if (!attributeList.Select(a => a.LogicalName).Contains(param.ParentLookupName))
								{
									throw new InvalidPluginExecutionException(String.Format("{0} is not a valid attribute name in {1} value", param.ParentLookupName, fields[field]));
								}

								if (attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Lookup && attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Customer && attributeList.Single(a => a.LogicalName.Equals(param.ParentLookupName)).AttributeType != AttributeTypeCode.Owner)
								{
									throw new InvalidPluginExecutionException(String.Format("{0} must be a Lookup attribute type in {1} value", param.ParentLookupName, fields[field]));
								}

								var parentLookupAttribute = (LookupAttributeMetadata)GetAttributeMetadata(Target.GetAttributeValue<string>("cel_entityname"), param.ParentLookupName);
								if (!parentLookupAttribute.Targets.Any(e => GetEntityMetadata(e).Select(a => a.LogicalName).Contains(param.AttributeName)))
								{
									throw new InvalidPluginExecutionException(String.Format("invalid attribute on {0} parent entity, in {1} value", param.ParentLookupName, fields[field]));
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
						throw new InvalidPluginExecutionException(String.Format("Failed to parse Runtime Parameters in {0} value.", fields[field]));
					}
				}
			}
#endif
			#endregion

			if (Target.Contains("cel_conditionaloptionset"))
			{
				Trace("Validate Conditional OptionSet");
				if (!attributeList.Select(a => a.LogicalName).Contains(Target.GetAttributeValue<string>("cel_conditionaloptionset")))
				{
					throw new InvalidPluginExecutionException("Specified Conditional OptionSet does not exist");
				}

				if (attributeList.Single(a => a.LogicalName.Equals(Target.GetAttributeValue<string>("cel_conditionaloptionset"))).AttributeType != AttributeTypeCode.Picklist)
				{
					throw new InvalidPluginExecutionException("Conditional Attribute must be an OptionSet");
				}

				Trace("Validate Conditional Value");
				PicklistAttributeMetadata optionSetMetadata = (PicklistAttributeMetadata)GetAttributeMetadata(Target.GetAttributeValue<string>("cel_entityname"), Target.GetAttributeValue<string>("cel_conditionaloptionset"));//attributeResponse.AttributeMetadata;
				if (!optionSetMetadata.OptionSet.Options.Select(o => o.Value).Contains(Target.GetAttributeValue<int>("cel_conditionalvalue")))
				{
					throw new InvalidPluginExecutionException("Conditional Value does not exist in OptionSet");
				}
			}

			#region Duplicate Check
#if DUPLICATECHECK
			Trace("Validate there are no duplicates");
			// TODO: Fix this. duplicate detection works when all fields contain data, but fails when some fields are empty
			var autoNumberList = Context.OrganizationDataContext.CreateQuery("cel_autonumber")
																.Where(a => a.GetAttributeValue<string>("cel_entityname").Equals(Target.GetAttributeValue<string>("cel_entityname")) && a.GetAttributeValue<string>("cel_attributename").Equals(Target.GetAttributeValue<string>("cel_attributename")))
																.Select(a => new { Id = a.GetAttributeValue<Guid>("cel_autonumberid"), ConditionalOption = a.GetAttributeValue<string>("cel_conditionaloptionset"), ConditionalValue = a.GetAttributeValue<int>("cel_conditionalvalue") })
																.ToList();


			if (!Target.Contains("cel_conditionaloptionset") && autoNumberList.Any())
			{
				throw new InvalidPluginExecutionException("Duplicate AutoNumber record exists.");
			}
			else if (autoNumberList.Where(a => a.ConditionalOption.Equals(Target.GetAttributeValue<string>("cel_conditionaloptionset")) && a.ConditionalValue.Equals(Target.GetAttributeValue<int>("cel_conditionalvalue"))).Any())
			{
				throw new InvalidPluginExecutionException("Duplicate AutoNumber record exists.");
			}
#endif
			#endregion

			Trace("Insert the autoNumber Name attribute");
			Target["cel_name"] = String.Format("AutoNumber for {0}, {1}", Target.GetAttributeValue<string>("cel_entityname"), Target.GetAttributeValue<string>("cel_attributename"));
		}

		private AttributeMetadata GetAttributeMetadata(string entityName, string attributeName)
		{
			string attributeKey = entityName + attributeName;
			if (!AttributeMetadata.ContainsKey(attributeKey))
			{
				try
				{
					RetrieveAttributeResponse attributeResponse = (RetrieveAttributeResponse)Context.OrganizationService.Execute(new RetrieveAttributeRequest() { EntityLogicalName = entityName, LogicalName = attributeName });
					AttributeMetadata.Add(attributeKey, attributeResponse.AttributeMetadata);
				}
				catch
				{
					throw new InvalidPluginExecutionException(String.Format("{1} attribute does not exist on {0} entity, or entity does not exist.", entityName, attributeName));
				}
			}

			return AttributeMetadata[attributeKey];
		}

		private List<AttributeMetadata> GetEntityMetadata(string entityName)
		{
			if (!EntityMetadata.ContainsKey(entityName))
			{
				try
				{
					RetrieveEntityResponse response = (RetrieveEntityResponse)Context.OrganizationDataContext.Execute(new RetrieveEntityRequest() { EntityFilters = EntityFilters.Attributes, LogicalName = entityName });
					EntityMetadata.Add(entityName, response.EntityMetadata.Attributes.ToList());  // Keep the list of Attributes
				}
				catch
				{
					throw new InvalidPluginExecutionException(String.Format("{0} Entity does not exist.", entityName));
				}
			}

			return EntityMetadata[entityName].ToList();
		}
	}
}
