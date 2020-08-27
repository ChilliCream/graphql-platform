using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

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
    }
}
