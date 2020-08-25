using HotChocolate.Language;
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
    }
}
