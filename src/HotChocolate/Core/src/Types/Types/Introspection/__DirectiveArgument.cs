#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

#nullable enable

namespace HotChocolate.Types.Introspection
{
    public class __DirectiveArgument : ObjectType<ArgumentNode>
    {
        protected override void Configure(
            IObjectTypeDescriptor<ArgumentNode> descriptor)
        {
            descriptor
                .Name(Names.__DirectiveArgument)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindFieldsExplicitly();

            descriptor
                .Field(Names.Name)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<ArgumentNode>().Name.Value);

            descriptor
                .Field(Names.Value)
                .Type<NonNullType<StringType>>()
                .Resolve(c => c.Parent<ArgumentNode>().Value.Print(indented: false));
        }

        public static class Names
        {
            public const string __DirectiveArgument = "__DirectiveArgument";
            public const string Name = "name";
            public const string Value = "value";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
