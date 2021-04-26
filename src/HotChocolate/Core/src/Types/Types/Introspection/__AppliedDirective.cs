#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    /// <summary>
    /// An Applied Directive is an instances of a directive as applied to a schema element.
    /// This type is NOT specified by the graphql specification presently.
    /// </summary>
    [Introspection]
    public class __AppliedDirective : ObjectType<DirectiveNode>
    {
        protected override void Configure(
            IObjectTypeDescriptor<DirectiveNode> descriptor)
        {
            descriptor
                .Name(Names.__AppliedDirective)
                .Description(TypeResources.___AppliedDirective_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFieldsExplicitly();

            descriptor
                .Field(Names.Name)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<DirectiveNode>().Name.Value);

            descriptor
                .Field(Names.Args)
                .Type<NonNullType<ListType<NonNullType<__DirectiveArgument>>>>()
                .Resolve(c => c.Parent<DirectiveNode>().Arguments);
        }

        public static class Names
        {
            public const string __AppliedDirective = "__AppliedDirective";
            public const string Args = "args";
            public const string Name = "name";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
