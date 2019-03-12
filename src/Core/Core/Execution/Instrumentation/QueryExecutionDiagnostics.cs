using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Instrumentation
{
    public sealed class QueryExecutionDiagnostics
    {
        private readonly DiagnosticSource _source;

        internal QueryExecutionDiagnostics(
            DiagnosticListener observable,
            IEnumerable<IDiagnosticObserver> observers)
        {
            _source = observable ??
                throw new ArgumentNullException(nameof(observable));

            Subscribe(observable, observers);
        }

        private static void Subscribe(
            DiagnosticListener observable,
            IEnumerable<IDiagnosticObserver> observers)
        {
            foreach (IDiagnosticObserver observer in observers)
            {
                observable.SubscribeWithAdapter(observer);
            }
        }

        public Activity BeginParsing(IQueryContext context)
        {
            var payload = new
            {
                context
            };

            if (_source.IsEnabled(DiagnosticNames.Parsing, payload))
            {
                var activity = new Activity(DiagnosticNames.Parsing);

                _source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginQuery(IQueryContext context)
        {
            var payload = new
            {
                context
            };

            if (_source.IsEnabled(DiagnosticNames.Query, payload))
            {
                var activity = new Activity(DiagnosticNames.Query);

                _source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginResolveField(
            IResolverContext context)
        {
            var payload = new
            {
                context
            };

            if (_source.IsEnabled(DiagnosticNames.Resolver, payload))
            {
                var activity = new Activity(DiagnosticNames.Resolver);

                _source.StartActivity(activity, payload);

                return activity;
            }

            return null;
        }

        public Activity BeginValidation(IQueryContext context)
        {
            var payload = new
            {
                context
            };

            if (_source.IsEnabled(DiagnosticNames.Validation, payload))
            {
                var activity = new Activity(DiagnosticNames.Validation);

                _source.StartActivity(activity, payload);

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
                    context
                };

                if (_source.IsEnabled(DiagnosticNames.Parsing, payload))
                {
                    _source.StopActivity(activity, payload);
                }
            }
        }

        public void EndQuery(Activity activity, IQueryContext context)
        {
            if (activity != null)
            {
                var payload = new
                {
                    context,
                    result = context.Result
                };

                if (_source.IsEnabled(DiagnosticNames.Query, payload))
                {
                    _source.StopActivity(activity, payload);
                }
            }
        }

        public void EndResolveField(
            Activity activity,
            IResolverContext context,
            object result)
        {
            if (activity != null)
            {
                var payload = new
                {
                    context,
                    result
                };

                if (_source.IsEnabled(DiagnosticNames.Resolver, payload))
                {
                    _source.StopActivity(activity, payload);
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
                    context,
                    result = context.ValidationResult
                };

                if (_source.IsEnabled(DiagnosticNames.Validation, payload))
                {
                    _source.StopActivity(activity, payload);
                }
            }
        }

        public void QueryError(IQueryContext context)
        {
            var payload = new
            {
                context,
                exception = context.Exception
            };

            if (_source.IsEnabled(DiagnosticNames.QueryError, payload))
            {
                _source.Write(DiagnosticNames.QueryError, payload);
            }
        }

        public void ResolverError(
            IResolverContext context,
            IError error)
        {
            ResolverError(context, new IError[] { error });
        }

        public void ResolverError(
            IResolverContext context,
            IEnumerable<IError> errors)
        {
            var payload = new
            {
                context,
                errors
            };

            if (_source.IsEnabled(DiagnosticNames.ResolverError, payload))
            {
                _source.Write(DiagnosticNames.ResolverError, payload);
            }
        }

        public void ValidationError(IQueryContext context)
        {
            var payload = new
            {
                context,
                errors = context.ValidationResult.Errors
            };

            if (_source.IsEnabled(DiagnosticNames.ValidationError, payload))
            {
                _source.Write(DiagnosticNames.ValidationError, payload);
            }
        }
    }
}
