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

// Removes the plugin step from an entity, if there are no registered autonumber records

using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xrm.Sdk;

namespace Celedon
{
	public class DeleteAutoNumber : CeledonPlugin
	{
		//
		// This plugin is executed when an AutoNumber record is deleted, it will remove the plugin steps from the associated entity
		//
		// Registration details:
		// Message: Delete
		// Primary Entity: cel_autonumber
		// User Context: SYSTEM
		// Event Pipeline: Post
		// Mode: Async
		// Config: none
		//
		// PreImage:
		// Name: PreImage
		// Alias: PreImage
		// Attributes: cel_entityname, cel_attributename
		//
		public DeleteAutoNumber()
		{
			//this.RegisteredEvents.Add(new Tuple<int,string,string,Action<LocalPluginContext>>(POSTOPERATION, DELETEMESSAGE, "entityname", new Action<LocalPluginContext>(Execute)));
			RegisterEvent(POSTOPERATION, DELETEMESSAGE, "cel_autonumber", Execute);
		}

		protected void Execute(LocalPluginContext Context)
		{
			var remainingAutoNumberList = Context.OrganizationDataContext.CreateQuery("cel_autonumber")
																		 .Where(s => s.GetAttributeValue<string>("cel_entityname").Equals(Context.PostImage.GetAttributeValue<string>("cel_entityname")))
																		 .Select(s => s.GetAttributeValue<Guid>("cel_autonumberid"))
																		 .ToList();

			if (remainingAutoNumberList.Any())  // If there are still other autonumber records on this entity, then do nothing.
			{
				return;  
			}

			// Find and remove the registerd plugin
			var pluginStepList = Context.OrganizationDataContext.CreateQuery("sdkmessageprocessingstep")
																.Where(s => s.GetAttributeValue<string>("name").Equals("CeledonPartners.AutoNumber." + Context.PostImage.GetAttributeValue<string>("cel_entityname")))
																.Select(s => s.GetAttributeValue<Guid>("sdkmessageprocessingstepid"))
																.ToList();

			if (!pluginStepList.Any())  // Plugin is already deleted, nothing to do here.
			{
				return;  
			}

			// Delete plugin step
			Context.OrganizationService.Delete("sdkmessageprocessingstep", pluginStepList.First());
		}
	}
}
