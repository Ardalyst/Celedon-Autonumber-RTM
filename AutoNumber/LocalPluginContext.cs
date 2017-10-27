/*The MIT License (MIT)

Copyright (c) 2017 Celedon Partners 

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
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Celedon
{
    public class LocalPluginContext : IDisposable
    {
        public IServiceProvider ServiceProvider
        {
            get;
        }

        public IOrganizationService OrganizationService
        {
            get;
        }

        public OrganizationServiceContext OrganizationDataContext
        {
            get;

            private set;
        }

        public IPluginExecutionContext PluginExecutionContext
        {
            get;
        }

        public ITracingService TracingService
        {
            get;
        }

        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Obtain the execution context service from the service provider.
            PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the tracing service from the service provider.
            TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the Organization Service factory service from the service provider
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Use the factory to generate the Organization Service.
            OrganizationService = factory.CreateOrganizationService(PluginExecutionContext.UserId);

            // Generate the Organization Data Context
            OrganizationDataContext = new OrganizationServiceContext(OrganizationService);
        }

        public Entity PreImage
        {
            get { try { return PluginExecutionContext.PreEntityImages.Values.First(); } catch { throw new InvalidPluginExecutionException("Pre Image Not Found"); } }
        }

        public Entity PostImage
        {
            get { try { return PluginExecutionContext.PostEntityImages.Values.First(); } catch { throw new InvalidPluginExecutionException("Post Image Not Found"); } }
        }

        public T GetInputParameters<T>() where T : class, CeledonPlugin.ICrmRequest
        {
            switch (PluginExecutionContext.MessageName)
            {
                case Constants.PipelineMessage.Create:
                    return new CeledonPlugin.CreateInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as Entity } as T;
                case Constants.PipelineMessage.Update:
                    return new CeledonPlugin.UpdateInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as Entity } as T;
                case Constants.PipelineMessage.Delete:
                    return new CeledonPlugin.DeleteInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as EntityReference } as T;
                case Constants.PipelineMessage.Retrieve:
                    return new CeledonPlugin.RetrieveInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as EntityReference, ColumnSet = PluginExecutionContext.InputParameters["ColumnSet"] as ColumnSet, RelatedEntitiesQuery = PluginExecutionContext.InputParameters["RelatedEntitiesQuery"] as RelationshipQueryCollection } as T;
                case Constants.PipelineMessage.RetrieveMultiple:
                    return new CeledonPlugin.RetrieveMultipleInputParameters() { Query = PluginExecutionContext.InputParameters["Query"] as QueryBase } as T;
                case Constants.PipelineMessage.Associate:
                    return new CeledonPlugin.AssociateInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as EntityReference, Relationship = PluginExecutionContext.InputParameters["Relationship"] as Relationship, RelatedEntities = PluginExecutionContext.InputParameters["RelatedEntities"] as EntityReferenceCollection } as T;
                case Constants.PipelineMessage.Disassociate:
                    return new CeledonPlugin.DisassociateInputParameters() { Target = PluginExecutionContext.InputParameters["Target"] as EntityReference, Relationship = PluginExecutionContext.InputParameters["Relationship"] as Relationship, RelatedEntities = PluginExecutionContext.InputParameters["RelatedEntities"] as EntityReferenceCollection } as T;
                case Constants.PipelineMessage.SetState:
                    return new CeledonPlugin.SetStateInputParameters() { EntityMoniker = PluginExecutionContext.InputParameters["EntityMoniker"] as EntityReference, State = PluginExecutionContext.InputParameters["State"] as OptionSetValue, Status = PluginExecutionContext.InputParameters["Status"] as OptionSetValue } as T;
                default:
                    return default(T);
            }
        }

        public T GetOutputParameters<T>() where T : class, CeledonPlugin.ICrmResponse
        {
            if (PluginExecutionContext.Stage < Constants.PipelineStage.PostOperation)
            {
                throw new InvalidOperationException("OutputParameters only exist during Post-Operation stage.");
            }

            switch (PluginExecutionContext.MessageName)
            {
                case Constants.PipelineMessage.Create:
                    return new CeledonPlugin.CreateOutputParameters() { Id = (Guid)PluginExecutionContext.OutputParameters["Id"] } as T;
                case Constants.PipelineMessage.Retrieve:
                    return new CeledonPlugin.RetrieveOutputParameters() { Entity = PluginExecutionContext.OutputParameters["Entity"] as Entity } as T;
                case Constants.PipelineMessage.RetrieveMultiple:
                    return new CeledonPlugin.RetrieveMultipleOutputParameters() { EntityCollection = PluginExecutionContext.OutputParameters["BusinessEntityCollection"] as EntityCollection } as T;
                default:
                    return default(T);
            }
        }

        public void Trace(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || TracingService == null)
            {
                return;
            }

            if (PluginExecutionContext == null)
            {
                TracingService.Trace(message);
            }
            else
            {
                TracingService.Trace("{0} : (Correlation Id: {1}, Initiating User: {2})", message, PluginExecutionContext.CorrelationId, PluginExecutionContext.InitiatingUserId);
            }
        }

        public void Dispose()
        {
            if (OrganizationDataContext == null)
            {
                return;
            }

            OrganizationDataContext.Dispose();
            OrganizationDataContext = null;
        }
    }
}
