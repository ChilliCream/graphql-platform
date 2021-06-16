#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using static HotChocolate.Types.Descriptors.TypeReference;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __InputValue : ObjectType
    {
        protected override ObjectTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            SyntaxTypeReference stringType = Create(ScalarNames.String);
            SyntaxTypeReference nonNullStringType = Parse($"{ScalarNames.String}!");
            SyntaxTypeReference nonNullTypeType = Parse($"{nameof(__Type)}!");
            SyntaxTypeReference appDirectiveListType = Parse($"[{nameof(__AppliedDirective)}!]!");

            var def = new ObjectTypeDefinition(
                Names.__InputValue,
                InputValue_Description,
                typeof(IInputField))
            {
                Fields =
                {
                    new(Names.Name, type: nonNullStringType, inlineResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, inlineResolver: Resolvers.Description),
                    new(Names.Type, type: nonNullTypeType, inlineResolver: Resolvers.Type),
                    new(Names.DefaultValue,
                        InputValue_DefaultValue,
                        stringType,
                        inlineResolver: Resolvers.DefaultValue),
                }
            };

            if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
            {
                def.Fields.Add(new(Names.AppliedDirectives,
                    type: appDirectiveListType,
                    inlineResolver: Resolvers.AppliedDirectives));
            }

            return def;
        }

        private static class Resolvers
        {
            public static object Name(object? parent)
                => ((IInputField)parent!).Name.Value;

            public static object? Description(object? parent)
                => ((IInputField)parent!).Description;

            public static object Type(object? parent)
                => ((IInputField)parent!).Type;

            public static object? DefaultValue(object? parent)
            {
                var field = (IInputField)parent!;
                return field.DefaultValue.IsNull()
                    ? null
                    : field.DefaultValue!.Print();
            }

            public static object AppliedDirectives(object? parent)
                => parent is IHasDirectives hasDirectives
                    ? hasDirectives.Directives.Where(t => t.Type.IsPublic).Select(d => d.ToNode())
                    : Enumerable.Empty<DirectiveNode>();
        }

        public static class Names
        {
            public const string __InputValue = "__InputValue";
            public const string Name = "name";
            public const string Description = "description";
            public const string DefaultValue = "defaultValue";
            public const string Type = "type";
            public const string AppliedDirectives = "appliedDirectives";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
