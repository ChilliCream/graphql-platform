using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Configuration
{
    internal static class SchemaContextFactory
    {
        public static SchemaContext Create()
        {
            var schemaContext = new SchemaContext();

            RegisterSpecScalarTypes(schemaContext);
            RegisterIntrospectionTypes(schemaContext);
            RegisterDirectives(schemaContext);

            return schemaContext;
        }

        private static void RegisterSpecScalarTypes(
            ISchemaContext schemaContext)
        {
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(StringType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(IdType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(BooleanType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(IntType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(FloatType)));
        }

        private static void RegisterIntrospectionTypes(
            ISchemaContext schemaContext)
        {
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__Directive)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__DirectiveLocation)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__EnumValue)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__Field)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__InputValue)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__Schema)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__Type)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(__TypeKind)));
        }

        private static void RegisterDirectives(
            ISchemaContext schemaContext)
        {
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(MultiplierPathType)));
            schemaContext.Directives.RegisterDirectiveType(
                new SkipDirectiveType());
            schemaContext.Directives.RegisterDirectiveType(
                new IncludeDirectiveType());
            schemaContext.Directives.RegisterDirectiveType(
                new CostDirectiveType());
        }
    }
}
