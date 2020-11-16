using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class ErrorHelper
    {
        public static ISchemaError CompleteInterfacesHelper_UnableToResolveInterface(
            ITypeSystemObject interfaceOrObject,
            ISyntaxNode? node)=>
            SchemaErrorBuilder.New()
                .SetMessage("COULD NOT RESOLVE INTERFACE")
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(interfaceOrObject)
                .AddSyntaxNode(node)
                .Build();

        public static ISchemaError DirectiveCollection_DirectiveIsUnique(
            DirectiveType directiveType,
            ITypeSystemObject type,
            DirectiveNode? syntaxNode,
            object source) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.DirectiveCollection_DirectiveIsUnique,
                    directiveType.Name)
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(type)
                .AddSyntaxNode(syntaxNode)
                .SetExtension("Source", source)
                .Build();

        public static ISchemaError DirectiveCollection_LocationNotAllowed(
            DirectiveType directiveType,
            Types.DirectiveLocation location,
            ITypeSystemObject type,
            DirectiveNode? syntaxNode,
            object source) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.DirectiveCollection_LocationNotAllowed,
                    directiveType.Name,
                    location)
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(type)
                .AddSyntaxNode(syntaxNode)
                .SetExtension("Source", source)
                .Build();

        public static ISchemaError DirectiveCollection_ArgumentValueTypeIsWrong(
            DirectiveType directiveType,
            ITypeSystemObject type,
            DirectiveNode? syntaxNode,
            object source,
            string argumentName) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The argument `{0}` value type is wrong.",
                    argumentName)
                .SetCode(ErrorCodes.Schema.ArgumentValueTypeWrong)
                .SetTypeSystemObject(directiveType)
                .AddSyntaxNode(syntaxNode)
                .SetExtension("Source", source)
                .Build();

        public static ISchemaError  DirectiveCollection_ArgumentDoesNotExist(
            DirectiveType directiveType,
            ITypeSystemObject type,
            DirectiveNode? syntaxNode,
            object source,
            string argumentName) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The argument `{0}` does not exist on the " +
                    "directive `{1}`.",
                    argumentName,
                    directiveType.Name)
                .SetCode(ErrorCodes.Schema.InvalidArgument)
                .SetTypeSystemObject(type)
                .AddSyntaxNode(syntaxNode)
                .SetExtension("Source", source)
                .Build();

        public static ISchemaError DirectiveCollection_ArgumentNonNullViolation(
            DirectiveType directiveType,
            ITypeSystemObject type,
            DirectiveNode? syntaxNode,
            object source,
            string argumentName) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "The argument `{0}` of directive `{1}` " +
                    "mustn't be null.",
                    argumentName,
                    directiveType.Name)
                .SetCode(ErrorCodes.Schema.NonNullArgument)
                .SetTypeSystemObject(type)
                .AddSyntaxNode(syntaxNode)
                .SetExtension("Source", source)
                .Build();

        public static ISchemaError ObjectType_UnableToInferOrResolveType(
            NameString typeName, ObjectType type, ObjectFieldDefinition field) =>
            SchemaErrorBuilder.New()
                .SetMessage(
                    "Unable to infer or resolve the type of " +
                    "field {0}.{1}. Try to explicitly provide the " +
                    "type like the following: " +
                    "`descriptor.Field(\"field\")" +
                    ".Type<List<StringType>>()`.",
                    typeName,
                    field.Name)
                .SetCode(ErrorCodes.Schema.NoFieldType)
                .SetTypeSystemObject(type)
                .SetPath(Path.New(typeName).Append(field.Name))
                .SetExtension(TypeErrorFields.Definition, field)
                .Build();
    }
}
