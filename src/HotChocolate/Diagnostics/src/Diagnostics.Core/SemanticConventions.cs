using System.Collections.Frozen;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

internal static class SemanticConventions
{
    public static class GraphQL
    {
        public static class Document
        {
            public const string Id = "graphql.document.id";
            public const string Hash = "graphql.document.hash";

            // Note: This is not part of the OTEL semantic conventions
            public const string Body = "graphql.document.body";
        }

        public static class Operation
        {
            public const string Name = "graphql.operation.name";
            public const string Type = "graphql.operation.type";

            // Note: This is not part of the OTEL semantic conventions
            public const string FieldCost = "graphql.operation.fieldCost";

            // Note: This is not part of the OTEL semantic conventions
            public const string TypeCost = "graphql.operation.typeCost";

            public static FrozenDictionary<OperationType, string> TypeValues { get; } =
                new Dictionary<OperationType, string>
                {
                    [OperationType.Query] = "query",
                    [OperationType.Mutation] = "mutation",
                    [OperationType.Subscription] = "subscription"
                }.ToFrozenDictionary();

            public static class Step
            {
                public const string Id = "graphql.operation.step.id";
                public const string Kind = "graphql.operation.step.kind";

                // Note: This is specific to Fusion
                public static class KindValues
                {
                    public const string Operation = "operation";
                    public const string OperationBatch = "operation_batch";
                    public const string Introspection = "introspection";
                    public const string Node = "node";
                }

                public static class Plan
                {
                    public const string Id = "graphql.operation.step.plan.id";
                }
            }
        }

        public static class Processing
        {
            public const string Type = "graphql.processing.type";

            public static class TypeValues
            {
                public const string Parse = "parse";
                public const string Validate = "validate";
                public const string VariableCoercion = "variable_coercion";
                public const string Plan = "plan";
                public const string Execute = "execute";
                public const string StepExecute = "step_execute";
                public const string Resolve = "resolve";
                public const string DataLoaderDispatch = "dataloader_dispatch";
                public const string DataLoaderBatch = "dataloader_batch";
            }
        }

        public static class Selection
        {
            public const string Name = "graphql.selection.name";
            public const string Path = "graphql.selection.path";

            public static class Field
            {
                public const string Name = "graphql.selection.field.name";
                public const string ParentType = "graphql.selection.field.parent_type";
                public const string Coordinate = "graphql.selection.field.coordinate";
            }
        }

        public static class DataLoader
        {
            public const string Name = "graphql.dataloader.name";

            public static class Batch
            {
                public const string Size = "graphql.dataloader.batch.size";
                public const string Keys = "graphql.dataloader.batch.keys";
            }

            public static class Cache
            {
                public const string HitCount = "graphql.dataloader.cache.hit_count";
                public const string MissCount = "graphql.dataloader.cache.miss_count";
            }
        }

        public static class Source
        {
            public const string Name = "graphql.source.name";
            public const string Id = "graphql.source.id";

            public static class Operation
            {
                public const string Name = "graphql.source.operation.name";
                public const string Kind = "graphql.source.operation.kind";
                public const string Hash = "graphql.source.operation.hash";
            }
        }

        // Note: This is not part of the OTEL semantic conventions
        public static class Errors
        {
            public const string Count = "graphql.errors.count";
        }

        public static class Error
        {
            public const string Message = "graphql.error.message";
            public const string Locations = "graphql.error.locations";
            public const string Path = "graphql.error.path";
            public const string Code = "graphql.error.code";

            public static class Location
            {
                public const string Column = "column";
                public const string Line = "line";
            }
        }

        public static class Subscription
        {
            public const string Id = "graphql.subscription.id";
        }

        // Note: This is not part of the OTEL semantic conventions
        public static class Http
        {
            public const string Kind = "graphql.http.kind";

            public static class Request
            {
                public const string Type = "graphql.http.request.type";
                public const string QueryId = "graphql.http.request.query.id";
                public const string QueryHash = "graphql.http.request.query.hash";
                public const string QueryBody = "graphql.http.request.query.body";
                public const string OperationName = "graphql.http.request.operation";
                public const string Operations = "graphql.http.request.operations";
                public const string Variables = "graphql.http.request.variables";
                public const string Extensions = "graphql.http.request.extensions";

                // Note: This is not part of the OTEL semantic conventions
                public static class Types
                {
                    public const string Single = "single";
                    public const string Batch = "batch";
                    public const string OperationBatch = "operation_batch";
                }

                // Note: This is not part of the OTEL semantic conventions
                public static class BatchRequest
                {
                    public static string QueryId(int index) => $"graphql.http.request[{index}].query.id";

                    public static string QueryHash(int index) => $"graphql.http.request[{index}].query.hash";

                    public static string QueryBody(int index) => $"graphql.http.request[{index}].query.body";

                    public static string OperationName(int index) => $"graphql.http.request[{index}].operation";

                    public static string Variables(int index) => $"graphql.http.request[{index}].variables";

                    public static string Extensions(int index) => $"graphql.http.request[{index}].extensions";
                }
            }
        }

        // Note: This is not part of the OTEL semantic conventions
        public static class Schema
        {
            public const string Name = "graphql.schema.name";
        }
    }
}
