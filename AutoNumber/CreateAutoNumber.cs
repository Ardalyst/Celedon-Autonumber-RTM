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

// Generates a plugin step for an entity, when a new autonumber record is created

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Celedon
{
	public class CreateAutoNumber : CeledonPlugin
	{
		//
		// This plugin is executed when a new AutoNumber record is created.  It generates the plugin steps on the entity type to create each number
		//
		// Registration Details:
		// Message: Create
		// Primary Entity: cel_autonumber
		// User Context: SYSTEM
		// Event Pipeline: Post
		// Mode: Async
		// Config: none
		//

		private const string PLUGIN_NAME = "CeledonPartners.AutoNumber.{0}";

		public CreateAutoNumber()
		{
			RegisterEvent(POSTOPERATION, CREATEMESSAGE, "cel_autonumber", Execute);
		}

		protected void Execute(LocalPluginContext Context)
		{
			Trace("Get Target record");
			Entity Target = Context.GetInputParameters<CreateInputParameters>().Target;
			string pluginName = String.Format(PLUGIN_NAME, Target.GetAttributeValue<string>("cel_entityname"));

			Trace("Check for existing plugin step");
			if (Context.OrganizationDataContext.CreateQuery("sdkmessageprocessingstep").Where(s => s.GetAttributeValue<string>("name").Equals(pluginName)).ToList().Any())
			{
				return;  // Step already exists, nothing to do here.
			}

			Trace("Get the Id of this plugin");
			Guid PluginTypeId = Context.OrganizationDataContext.CreateQuery("plugintype")
				 											   .Where(s => s.GetAttributeValue<string>("name").Equals("Celedon.GetNextAutoNumber"))
															   .Select(s => s.GetAttributeValue<Guid>("plugintypeid"))
															   .First();

			Trace("Get the 'Create' message id from this org");
			Guid messageId = Context.OrganizationDataContext.CreateQuery("sdkmessage")  
															.Where(s => s.GetAttributeValue<string>("name").Equals("Create"))
															.Select(s => s.GetAttributeValue<Guid>("sdkmessageid"))
															.First();

			Trace("Get the filterId for 'Create' for the specific entity from this org");
			Guid filterId = Context.OrganizationDataContext.CreateQuery("sdkmessagefilter")  
														   .Where(s => s.GetAttributeValue<string>("primaryobjecttypecode").Equals(Target.GetAttributeValue<string>("cel_entityname"))
															   && s.GetAttributeValue<EntityReference>("sdkmessageid").Id.Equals(messageId))
														   .Select(s => s.GetAttributeValue<Guid>("sdkmessagefilterid"))
														   .First();

			Trace("Build new plugin step");
			Entity newPluginStep = new Entity("sdkmessageprocessingstep")
			{
				Attributes = new AttributeCollection()
				{
					{ "name", pluginName },
					{ "description", pluginName },
					{ "plugintypeid", PluginTypeId.ToEntityReference("plugintype") },  // This plugin
					{ "sdkmessageid", messageId.ToEntityReference("sdkmessage") },  // Create Message
					{ "configuration", Target.GetAttributeValue<string>("cel_entityname") },  // EntityName in the UnsecureConfig
					{ "stage", PREOPERATION.ToOptionSetValue() },  // Execution Stage: Pre-Operation
					{ "rank", 1 },
					{ "impersonatinguserid", Context.PluginExecutionContext.UserId.ToEntityReference("systemuser") },  // Run as SYSTEM user. Assumes we are currently running as the SYSTEM user
					{ "sdkmessagefilterid", filterId.ToEntityReference("sdkmessagefilter") }
				}
			};

			Trace("Create new plugin step");
			Guid pluginStepId = Context.OrganizationService.Create(newPluginStep);
		}
	}
}
