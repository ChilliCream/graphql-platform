using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    public sealed class DiagnosticEvents
    {
        internal readonly DiagnosticListener Listener =
            new DiagnosticListener(DiagnosticNames.Listener);

        public Activity BeginParsing(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (Listener.IsEnabled(DiagnosticNames.Parsing, payload))
            {
                var activity = new Activity(DiagnosticNames.Parsing);

                Listener.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginQuery(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (Listener.IsEnabled(DiagnosticNames.Query, payload))
            {
                var activity = new Activity(DiagnosticNames.Query);

                Listener.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginResolveField(
            IResolverContext resolverContext)
        {
            var payload = new
            {
                context = resolverContext
            };

            if (Listener.IsEnabled(DiagnosticNames.Resolver, payload))
            {
                var activity = new Activity(DiagnosticNames.Resolver);

                Listener.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginValidation(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (Listener.IsEnabled(DiagnosticNames.Validation, payload))
            {
                var activity = new Activity(DiagnosticNames.Validation);

                Listener.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public void EndParsing(Activity activity, IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document
                };

                if (Listener.IsEnabled(DiagnosticNames.Parsing, payload))
                {
                    Listener.StopActivity(activity, payload);
                }
            }
        }

        public void EndQuery(Activity activity, IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document,
                    result = context.Result
                };

                if (Listener.IsEnabled(DiagnosticNames.Query, payload))
                {
                    Listener.StopActivity(activity, payload);
                }
            }
        }

        public void EndResolveField(
            Activity activity,
            IResolverContext resolverContext,
            object resolvedValue)
        {
            if (activity != null)
            {
                var payload = new
                {
                    context = resolverContext,
                    result = resolvedValue
                };

                if (Listener.IsEnabled(DiagnosticNames.Resolver, payload))
                {
                    Listener.StopActivity(activity, payload);
                }
            }
        }

        public void EndValidation(
            Activity activity,
            IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document,
                    result = context.ValidationResult
                };

                if (Listener.IsEnabled(DiagnosticNames.Validation, payload))
                {
                    Listener.StopActivity(activity, payload);
                }
            }
        }

        public void QueryError(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                exception = context.Exception
            };

            if (Listener.IsEnabled(DiagnosticNames.QueryError, payload))
            {
                Listener.Write(DiagnosticNames.QueryError, payload);
            }
        }

        public void ResolverError(
            IResolverContext resolverContext,
            Exception exception)
        {
            var payload = new
            {
                context = resolverContext,
                exception
            };

            if (Listener.IsEnabled(DiagnosticNames.ResolverError, payload))
            {
                Listener.Write(DiagnosticNames.ResolverError, payload);
            }
        }

        public void ValidationError(IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                errors = context.ValidationResult.Errors
            };

            if (Listener.IsEnabled(DiagnosticNames.ValidationError, payload))
            {
                Listener.Write(DiagnosticNames.ValidationError, payload);
            }
        }
    }
}
