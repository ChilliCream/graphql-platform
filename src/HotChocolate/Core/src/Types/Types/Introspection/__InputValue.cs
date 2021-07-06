#pragma warning disable IDE1006 // Naming Styles
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
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
                    new(Names.Name, type: nonNullStringType, pureResolver: Resolvers.Name),
                    new(Names.Description, type: stringType, pureResolver: Resolvers.Description),
                    new(Names.Type, type: nonNullTypeType, pureResolver: Resolvers.Type),
                    new(Names.DefaultValue,
                        InputValue_DefaultValue,
                        stringType,
                        pureResolver: Resolvers.DefaultValue),
                }
            };

            if (context.DescriptorContext.Options.EnableDirectiveIntrospection)
            {
                def.Fields.Add(new(
                    Names.AppliedDirectives,
                    type: appDirectiveListType,
                    pureResolver: Resolvers.AppliedDirectives));
            }

            return def;
        }

        private static class Resolvers
        {
            public static object Name(IPureResolverContext context)
                => context.Parent<IInputField>().Name.Value;

            public static object? Description(IPureResolverContext context)
                => context.Parent<IInputField>().Description;

            public static object Type(IPureResolverContext context)
                => context.Parent<IInputField>().Type;

            public static object? DefaultValue(IPureResolverContext context)
            {
                IInputField field = context.Parent<IInputField>();
                return field.DefaultValue.IsNull() ? null : field.DefaultValue!.Print();
            }

            public static object AppliedDirectives(IPureResolverContext context)
                => context.Parent<IHasDirectives>().Directives
                    .Where(t => t.Type.IsPublic)
                    .Select(d => d.ToNode());
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
