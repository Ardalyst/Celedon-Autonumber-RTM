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

using System;
using System.Linq;
using System.ServiceModel;
using System.Collections.ObjectModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
	/// <inheritdoc />
	///  <summary>
	///  Base class for all Plugins.
	///  Modified by Matt Barnes - "I've made a few special modifications myself"
	///  - Added the method RegisterEvent(), to simplify each child plugin constructor
	///  - Removed the base class constructor with the child class argument (it wasn't needed)
	///  - LocalPluginContext class now includes an OrganizationServiceContext object, so it doesn't need to be initialized within each plugin
	///  - LocalPluginContext class now implements IDisposable, so the OrganizationServiceContext object gets properly disposed when it is done
	///  - Added the Execution Stage constants
	///  - Added GetInputParameters() method for early binding of InputParameters
	///  - Added GetOutputParameters() method for early binding of OutputParameters
	///  - Added PreImage and PostImage - returns the first available image (assumes there is only one) if you have multiple, then retrieve them normally
	///  </summary>    
	public abstract class CeledonPlugin : IPlugin
	{
	    /// <summary>
		/// Gets the List of events that the plug-in should fire for. Each List
		/// Item is a <see cref="System.Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
		/// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
		/// </summary>
		private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents { get; } = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();

	    protected void RegisterEvent(int stage, string eventName, string entityName, Action<LocalPluginContext> executeMethod)
		{
			RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(stage, eventName, entityName, executeMethod));
		}

		protected delegate void TraceDelegate(string message);
		// protected TraceDelegate Trace;

		/// <summary>
		/// Gets or sets the name of the child class.
		/// </summary>
		/// <value>The name of the child class.</value>
		protected string ChildClassName => GetType().Name;

		/// <inheritdoc />
		/// <summary>
		/// Executes the plug-in.
		/// </summary>
		/// <param name="serviceProvider">The service provider.</param>
		/// <remarks>
		/// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
		/// The plug-in's Execute method should be written to be stateless as the constructor 
		/// is not called for every invocation of the plug-in. Also, multiple system threads 
		/// could execute the plug-in at the same time. All per invocation state information 
		/// is stored in the context. This means that you should not use global variables in plug-ins.
		/// </remarks>
		public void Execute(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null)
			{
				throw new ArgumentNullException(nameof(serviceProvider));
			}

			// Construct the Local plug-in context.
			using (var localContext = new LocalPluginContext(serviceProvider))
			{
				localContext.Trace($"Entered {ChildClassName}.Execute()");

				try
				{
					// Iterate over all of the expected registered events to ensure that the plugin
					// has been invoked by an expected event
					// For any given plug-in event at an instance in time, we would expect at most 1 result to match.
					var entityAction =
						(from a in RegisteredEvents
						 where (
							 a.Item1 == localContext.PluginExecutionContext.Stage &&
							 a.Item2 == localContext.PluginExecutionContext.MessageName &&
							 (string.IsNullOrWhiteSpace(a.Item3) || a.Item3 == localContext.PluginExecutionContext.PrimaryEntityName)
						 )
						 select a.Item4).FirstOrDefault();

				    if (entityAction == null)
				    {
				        return;
				    }

				    localContext.Trace($"{ChildClassName} is firing for Entity: {localContext.PluginExecutionContext.PrimaryEntityName}, Message: {localContext.PluginExecutionContext.MessageName}");

				    entityAction.Invoke(localContext);
				}
				catch (FaultException<OrganizationServiceFault> e)
				{
					localContext.Trace($"Exception: {e}");
					throw;
				}
				finally
				{
					localContext.Trace($"Exiting {ChildClassName}.Execute()");
				}
			}
		}

	    public interface ICrmRequest { }

	    public class CreateInputParameters : ICrmRequest
		{
			internal Entity Target;
		}

	    public class UpdateInputParameters : ICrmRequest
		{
			internal Entity Target;
		}

	    public class DeleteInputParameters : ICrmRequest
		{
			internal EntityReference Target;
		}

	    public class RetrieveInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal ColumnSet ColumnSet;
			internal RelationshipQueryCollection RelatedEntitiesQuery;
		}

	    public class RetrieveMultipleInputParameters : ICrmRequest
		{
			internal QueryBase Query;
		}

	    public class AssociateInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal Relationship Relationship;
			internal EntityReferenceCollection RelatedEntities;
		}

	    public class DisassociateInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal Relationship Relationship;
			internal EntityReferenceCollection RelatedEntities;
		}

	    public class SetStateInputParameters : ICrmRequest
		{
			internal EntityReference EntityMoniker;
			internal OptionSetValue State;
			internal OptionSetValue Status;
		}

	    public interface ICrmResponse { }

	    public class CreateOutputParameters : ICrmResponse
		{
			internal Guid Id;
		}

	    public class UpdateOutputParameters : ICrmResponse { }

	    public class DeleteOutputParameters : ICrmResponse { }

	    public class RetrieveOutputParameters : ICrmResponse
		{
			internal Entity Entity;
		}

	    public class RetrieveMultipleOutputParameters : ICrmResponse
		{
			internal EntityCollection EntityCollection;
		}

	    public class AssociateOutputParameters : ICrmResponse { }

	    public class DisassociateOutputParameters : ICrmResponse { }

	    public class SetStateOutputParameters : ICrmResponse { }
	}
}