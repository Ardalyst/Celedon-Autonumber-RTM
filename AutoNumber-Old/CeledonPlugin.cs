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

// Implements the Plugin Workflow Activity.

using System;
using System.Linq;
using System.ServiceModel;
using System.Globalization;
using System.Collections.ObjectModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
	/// <summary>
	/// Base class for all Plugins.
	///
	/// Modified by Matt Barnes - "I've made a few special modifications myself"
	/// 
	/// - Added the method RegisterEvent(), to simplify each child plugin constructor
	/// - Removed the base class constructor with the child class argument (it wasn't needed)
	/// - LocalPluginContext class now includes an OrganizationServiceContext object, so it doesn't need to be initialized within each plugin
	/// - LocalPluginContext class now implements IDisposable, so the OrganizationServiceContext object gets properly disposed when it is done
	/// - Added the Execution Stage constants
	/// - Added GetInputParameters() method for early binding of InputParameters
	/// - Added GetOutputParameters() method for early binding of OutputParameters
	/// - Added PreImage and PostImage - returns the first available image (assumes there is only one) if you have multiple, then retrieve them normally
	/// 
	/// </summary>    
	public class CeledonPlugin : IPlugin
	{
		public const int PREVALIDATION = 10;
		public const int PREOPERATION = 20;
		public const int POSTOPERATION = 40;

		public const string CREATEMESSAGE = "Create";
		public const string RETRIEVEMESSAGE = "Retrieve";
		public const string UPDATEMESSAGE = "Update";
		public const string DELETEMESSAGE = "Delete";
		public const string RETRIEVEMULTIPLEMESSAGE = "RetrieveMultiple";
		public const string ASSOCIATEMESSAGE = "Associate";
		public const string DISASSOCIATEMESSAGE = "Disassociate";
		public const string SETSTATEMESSAGE = "SetState";

		protected class LocalPluginContext : IDisposable
		{
			internal IServiceProvider ServiceProvider
			{
				get;

				private set;
			}

			internal IOrganizationService OrganizationService
			{
				get;

				private set;
			}

			internal OrganizationServiceContext OrganizationDataContext
			{
				get;

				private set;
			}

			internal IPluginExecutionContext PluginExecutionContext
			{
				get;

				private set;
			}

			internal ITracingService TracingService
			{
				get;

				private set;
			}

			private LocalPluginContext() { }

			internal LocalPluginContext(IServiceProvider serviceProvider)
			{
				if (serviceProvider == null)
				{
					throw new ArgumentNullException("serviceProvider");
				}

				// Obtain the execution context service from the service provider.
				this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

				// Obtain the tracing service from the service provider.
				this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

				// Obtain the Organization Service factory service from the service provider
				IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

				// Use the factory to generate the Organization Service.
				this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);

				// Generate the Organization Data Context
				this.OrganizationDataContext = new OrganizationServiceContext(this.OrganizationService);
			}

			internal Entity PreImage
			{
				get { try { return this.PluginExecutionContext.PreEntityImages.Values.First(); } catch { throw new InvalidPluginExecutionException("Pre Image Not Found"); } }
			}

			internal Entity PostImage
			{
				get { try { return this.PluginExecutionContext.PostEntityImages.Values.First(); } catch { throw new InvalidPluginExecutionException("Post Image Not Found"); } }
			}

			internal T GetInputParameters<T>() where T : class, ICrmRequest
			{
				switch (this.PluginExecutionContext.MessageName)
				{
					case CREATEMESSAGE:
						return new CreateInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as Entity } as T;
					case UPDATEMESSAGE:
						return new UpdateInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as Entity } as T;
					case DELETEMESSAGE:
						return new DeleteInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as EntityReference } as T;
					case RETRIEVEMESSAGE:
						return new RetrieveInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as EntityReference, ColumnSet = this.PluginExecutionContext.InputParameters["ColumnSet"] as ColumnSet, RelatedEntitiesQuery = this.PluginExecutionContext.InputParameters["RelatedEntitiesQuery"] as RelationshipQueryCollection } as T;
					case RETRIEVEMULTIPLEMESSAGE:
						return new RetrieveMultipleInputParameters() { Query = this.PluginExecutionContext.InputParameters["Query"] as QueryBase } as T;
					case ASSOCIATEMESSAGE:
						return new AssociateInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as EntityReference, Relationship = this.PluginExecutionContext.InputParameters["Relationship"] as Relationship, RelatedEntities = this.PluginExecutionContext.InputParameters["RelatedEntities"] as EntityReferenceCollection } as T;
					case DISASSOCIATEMESSAGE:
						return new DisassociateInputParameters() { Target = this.PluginExecutionContext.InputParameters["Target"] as EntityReference, Relationship = this.PluginExecutionContext.InputParameters["Relationship"] as Relationship, RelatedEntities = this.PluginExecutionContext.InputParameters["RelatedEntities"] as EntityReferenceCollection } as T;
					case SETSTATEMESSAGE:
						return new SetStateInputParameters() { EntityMoniker = this.PluginExecutionContext.InputParameters["EntityMoniker"] as EntityReference, State = this.PluginExecutionContext.InputParameters["State"] as OptionSetValue, Status = this.PluginExecutionContext.InputParameters["Status"] as OptionSetValue } as T;
					default:
						return default(T);
				}
			}

			internal T GetOutputParameters<T>() where T : class, ICrmResponse
			{
				if (this.PluginExecutionContext.Stage < POSTOPERATION)
				{
					throw new InvalidOperationException("OutputParameters only exist during Post-Operation stage.");
				}

				switch (this.PluginExecutionContext.MessageName)
				{
					case CREATEMESSAGE:
						return new CreateOutputParameters() { Id = (Guid)this.PluginExecutionContext.OutputParameters["Id"] } as T;
					case RETRIEVEMESSAGE:
						return new RetrieveOutputParameters() { Entity = this.PluginExecutionContext.OutputParameters["Entity"] as Entity } as T;
					case RETRIEVEMULTIPLEMESSAGE:
						return new RetrieveMultipleOutputParameters() { EntityCollection = this.PluginExecutionContext.OutputParameters["BusinessEntityCollection"] as EntityCollection } as T;
					default:
						return default(T);
				}
			}

			internal void Trace(string message)
			{
				if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
				{
					return;
				}

				if (this.PluginExecutionContext == null)
				{
					this.TracingService.Trace(message);
				}
				else
				{
					this.TracingService.Trace("{0} : (Correlation Id: {1}, Initiating User: {2})", message, this.PluginExecutionContext.CorrelationId, this.PluginExecutionContext.InitiatingUserId);
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (OrganizationDataContext != null)
					{
						OrganizationDataContext.Dispose();
						OrganizationDataContext = null;
					}
				}
			}
		}

		private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> registeredEvents;

		/// <summary>
		/// Gets the List of events that the plug-in should fire for. Each List
		/// Item is a <see cref="System.Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
		/// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
		/// </summary>
		protected Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents
		{
			get
			{
				if (this.registeredEvents == null)
				{
					this.registeredEvents = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();
				}

				return this.registeredEvents;
			}
		}

		protected void RegisterEvent(int Stage, string EventName, string EntityName, Action<LocalPluginContext> ExecuteMethod)
		{
			this.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(Stage, EventName, EntityName, ExecuteMethod));
		}

		protected delegate void TraceDelegate(string message);
		protected TraceDelegate Trace;

		/// <summary>
		/// Gets or sets the name of the child class.
		/// </summary>
		/// <value>The name of the child class.</value>
		protected string ChildClassName
		{
			get { return this.GetType().Name; }

			//private set;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Plugin"/> class.
		/// </summary>
		/// <param name="childClassName">The <see cref=" cred="Type"/> of the derived class.</param>
		//internal Plugin(Type childClassName)
		//{
		//	this.ChildClassName = childClassName.ToString();
		//}

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
				throw new ArgumentNullException("serviceProvider");
			}

			// Construct the Local plug-in context.
			using (LocalPluginContext localcontext = new LocalPluginContext(serviceProvider))
			{
				localcontext.Trace(String.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.ChildClassName));

				try
				{
					Trace = m => localcontext.Trace(m);
					// Iterate over all of the expected registered events to ensure that the plugin
					// has been invoked by an expected event
					// For any given plug-in event at an instance in time, we would expect at most 1 result to match.
					Action<LocalPluginContext> entityAction =
						(from a in this.RegisteredEvents
						 where (
							 a.Item1 == localcontext.PluginExecutionContext.Stage &&
							 a.Item2 == localcontext.PluginExecutionContext.MessageName &&
							 (String.IsNullOrWhiteSpace(a.Item3) ? true : a.Item3 == localcontext.PluginExecutionContext.PrimaryEntityName)
						 )
						 select a.Item4).FirstOrDefault();

					if (entityAction != null)
					{
						localcontext.Trace(String.Format(CultureInfo.InvariantCulture, "{0} is firing for Entity: {1}, Message: {2}", this.ChildClassName, localcontext.PluginExecutionContext.PrimaryEntityName, localcontext.PluginExecutionContext.MessageName));

						entityAction.Invoke(localcontext);

						// now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
						// guard against multiple executions.
						return;
					}
				}
				catch (FaultException<OrganizationServiceFault> e)
				{
					localcontext.Trace(String.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

					// Handle the exception.
					throw;
				}
				finally
				{
					localcontext.Trace(String.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.ChildClassName));
				}
			}
		}

		internal interface ICrmRequest { }

		internal class CreateInputParameters : ICrmRequest
		{
			internal Entity Target;
		}

		internal class UpdateInputParameters : ICrmRequest
		{
			internal Entity Target;
		}

		internal class DeleteInputParameters : ICrmRequest
		{
			internal EntityReference Target;
		}

		internal class RetrieveInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal ColumnSet ColumnSet;
			internal RelationshipQueryCollection RelatedEntitiesQuery;
		}

		internal class RetrieveMultipleInputParameters : ICrmRequest
		{
			internal QueryBase Query;
		}

		internal class AssociateInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal Relationship Relationship;
			internal EntityReferenceCollection RelatedEntities;
		}

		internal class DisassociateInputParameters : ICrmRequest
		{
			internal EntityReference Target;
			internal Relationship Relationship;
			internal EntityReferenceCollection RelatedEntities;
		}

		internal class SetStateInputParameters : ICrmRequest
		{
			internal EntityReference EntityMoniker;
			internal OptionSetValue State;
			internal OptionSetValue Status;
		}

		internal interface ICrmResponse { }

		internal class CreateOutputParameters : ICrmResponse
		{
			internal Guid Id;
		}

		internal class UpdateOutputParameters : ICrmResponse { }

		internal class DeleteOutputParameters : ICrmResponse { }

		internal class RetrieveOutputParameters : ICrmResponse
		{
			internal Entity Entity;
		}

		internal class RetrieveMultipleOutputParameters : ICrmResponse
		{
			internal EntityCollection EntityCollection;
		}

		internal class AssociateOutputParameters : ICrmResponse { }

		internal class DisassociateOutputParameters : ICrmResponse { }

		internal class SetStateOutputParameters : ICrmResponse { }
	}
}