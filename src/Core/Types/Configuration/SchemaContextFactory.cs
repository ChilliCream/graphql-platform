using HotChocolate.Utilities;
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
            RegisterExtendedScalarTypes(schemaContext);
            RegisterIntrospectionTypes(schemaContext);
            RegisterDirectives(schemaContext);

            return schemaContext;
        }

        private static void RegisterSpecScalarTypes(
            SchemaContext schemaContext)
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

        private static void RegisterExtendedScalarTypes(
            SchemaContext schemaContext)
        {
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(DecimalType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(LongType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(DateTimeType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(DateType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(UuidType)));
            schemaContext.Types.RegisterType(
                new TypeReference(typeof(UrlType)));
        }

        private static void RegisterIntrospectionTypes(
            SchemaContext schemaContext)
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
            SchemaContext schemaContext)
        {
            schemaContext.Directives.RegisterDirectiveType(new SkipDirective());
            schemaContext.Directives.RegisterDirectiveType(new IncludeDirectiveType());
        }
    }
}
