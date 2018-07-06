using System;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate
{
    public partial class Schema
    {
        private sealed class IntrospectionFields
        {
            public IntrospectionFields(
                SchemaContext schemaContext,
                Action<SchemaError> reportError)
            {
                SchemaField = new __SchemaField();
                TypeField = new __TypeField();
                TypeNameField = new __TypeNameField();

                ObjectType schema = schemaContext.Types.GetType<ObjectType>("__Schema");
                var initializationContext = new TypeInitializationContext(
                    schemaContext, reportError, schema);
                CompleteField(initializationContext, SchemaField);
                CompleteField(initializationContext, TypeField);
                CompleteField(initializationContext, TypeNameField);
            }

            private static void CompleteField(
                TypeInitializationContext context,
                INeedsInitialization field)
            {
                field.RegisterDependencies(context);
                field.CompleteType(context);
            }

            internal __SchemaField SchemaField { get; }

            internal __TypeField TypeField { get; }

            internal __TypeNameField TypeNameField { get; }
        }
    }
}
