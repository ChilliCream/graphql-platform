using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class TestUtils
    {
        public static string CreateTypeName()
        {
            return "Type_" + Guid.NewGuid().ToString("N");
        }

        public static string CreateFieldName()
        {
            return "field_" + Guid.NewGuid().ToString("N");
        }

        internal static TypeResult<T> CreateType<T>(
            Func<ISchemaContext, T> factory)
            where T : class, INamedType, INeedsInitialization
        {
            var errors = new List<SchemaError>();
            var schemaContext = new SchemaContext();

            var type = factory(schemaContext);
            schemaContext.Types.RegisterType(type);

            var initializationContext = new TypeInitializationContext(
                schemaContext, a => errors.Add(a), type, false);
            type.RegisterDependencies(initializationContext);

            schemaContext.CompleteTypes();

            return new TypeResult<T>(type, errors);
        }
    }

    public class TypeResult<T>
        where T : class, INamedType
    {
        public TypeResult(T type, IReadOnlyList<SchemaError> errors)
        {
            Type = type;
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public T Type { get; }

        public IReadOnlyList<SchemaError> Errors { get; }
    }
}
