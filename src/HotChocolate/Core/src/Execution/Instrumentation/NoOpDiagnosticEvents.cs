using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    internal sealed class NoopDiagnosticEvents
        : IDiagnosticEvents
        , IActivityScope
    {
        public IActivityScope ExecuteRequest(IRequestContext context) => this;

        public void RequestError(IRequestContext context, Exception exception)
        {
        }

        public IActivityScope ParseDocument(IRequestContext context) => this;

        public void SyntaxError(IRequestContext context, IError error)
        {
        }

        public IActivityScope ValidateDocument(IRequestContext context) => this;

        public void ValidationErrors(IRequestContext context, IReadOnlyList<IError> errors)
        {
        }

        public IActivityScope ResolveFieldValue(IMiddlewareContext context) => this;

        public void ResolverError(IMiddlewareContext context, IError error)
        {
        }

        public IActivityScope RunTask(IExecutionTask task) => this;

        public void TaskError(IExecutionTask task, IError error)
        {
        }

        public IActivityScope ExecuteSubscription(ISubscription subscription) => this;

        public IActivityScope OnSubscriptionEvent(SubscriptionEventContext context) => this;

        public void SubscriptionEventResult(SubscriptionEventContext context, IQueryResult result)
        {
        }

        public void SubscriptionEventError(SubscriptionEventContext context, Exception exception)
        {
        }

        public void SubscriptionTransportError(ISubscription subscription, Exception exception)
        {
        }

        public void AddedDocumentToCache(IRequestContext context)
        {
        }

        public void RetrievedDocumentFromCache(IRequestContext context)
        {
        }

        public void RetrievedDocumentFromStorage(IRequestContext context)
        {
        }

        public void AddedOperationToCache(IRequestContext context)
        {
        }

        public void RetrievedOperationFromCache(IRequestContext context)
        {
        }

        public void BatchDispatched(IRequestContext context)
        {
        }

        public void ExecutorCreated(string name, IRequestExecutor executor)
        {
        }

        public void ExecutorEvicted(string name, IRequestExecutor executor)
        {
        }

        public void Dispose()
        {
        }
    }
}
