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
            public IntrospectionFields(SchemaContext context, Action<SchemaError> reportError)
            {
                SchemaField = new __SchemaField();
                TypeField = new __TypeField();
                TypeNameField = new __TypeNameField();

                SchemaField.RegisterDependencies(context, reportError, null);
                TypeField.RegisterDependencies(context, reportError, null);
                TypeNameField.RegisterDependencies(context, reportError, null);

                ObjectType schema = context.Types.GetType<ObjectType>("__Schema");
                SchemaField.CompleteField(context, reportError, schema);
                TypeField.CompleteField(context, reportError, schema);
                TypeNameField.CompleteField(context, reportError, schema);
            }

            internal __SchemaField SchemaField { get; }

            internal __TypeField TypeField { get; }

            internal __TypeNameField TypeNameField { get; }
        }
    }
}
