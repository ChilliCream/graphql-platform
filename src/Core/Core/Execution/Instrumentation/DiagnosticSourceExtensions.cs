using System;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    internal static class DiagnosticSourceExtensions
    {
        public static Activity BeginExecute(
            this DiagnosticSource source,
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (source.IsEnabled(DiagnosticNames.Query, payload))
            {
                var activity = new Activity(DiagnosticNames.Query);

                source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static Activity BeginParsing(
            this DiagnosticSource source,
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (source.IsEnabled(DiagnosticNames.Parsing, payload))
            {
                var activity = new Activity(DiagnosticNames.Parsing);

                source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static Activity BeginResolveField(
            this DiagnosticSource source,
            IResolverContext resolverContext)
        {
            var payload = new
            {
                context = resolverContext
            };

            if (source.IsEnabled(DiagnosticNames.Resolver, payload))
            {
                var activity = new Activity(DiagnosticNames.Resolver);

                source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static Activity BeginValidation(
            this DiagnosticSource source,
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document
            };

            if (source.IsEnabled(DiagnosticNames.Validation, payload))
            {
                var activity = new Activity(DiagnosticNames.Validation);

                source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public static void EndExecute(
            this DiagnosticSource source,
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
                    result = context.Result
                };

                if (source.IsEnabled(DiagnosticNames.Query, payload))
                {
                    source.StopActivity(activity, payload);
                }
            }
        }

        public static void EndParsing(
            this DiagnosticSource source,
            Activity activity,
            IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    schema = context.Schema,
                    request = context.Request,
                    query = context.Document
                };

                if (source.IsEnabled(DiagnosticNames.Parsing, payload))
                {
                    source.StopActivity(activity, payload);
                }
            }
        }

        public static void EndResolveField(
            this DiagnosticSource source,
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

                if (source.IsEnabled(DiagnosticNames.Resolver, payload))
                {
                    source.StopActivity(activity, payload);
                }
            }
        }

        public static void EndValidation(
            this DiagnosticSource source,
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

                if (source.IsEnabled(DiagnosticNames.Validation, payload))
                {
                    source.StopActivity(activity, payload);
                }
            }
        }

        public static void QueryError(
            this DiagnosticSource source,
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                exception = context.Exception
            };

            if (source.IsEnabled(DiagnosticNames.QueryError, payload))
            {
                source.Write(DiagnosticNames.QueryError, payload);
            }
        }

        public static void ResolverError(
            this DiagnosticSource source,
            IResolverContext resolverContext,
            Exception exception)
        {
            var payload = new
            {
                context = resolverContext,
                exception
            };

            if (source.IsEnabled(DiagnosticNames.ResolverError, payload))
            {
                source.Write(DiagnosticNames.ResolverError, payload);
            }
        }

        public static void ValidationError(
            this DiagnosticSource source,
            IQueryContext context)
        {
            var payload = new
            {
                schema = context.Schema,
                request = context.Request,
                query = context.Document,
                errors = context.ValidationResult.Errors
            };

            if (source.IsEnabled(DiagnosticNames.ValidationError, payload))
            {
                source.Write(DiagnosticNames.ValidationError, payload);
            }
        }
    }
}
