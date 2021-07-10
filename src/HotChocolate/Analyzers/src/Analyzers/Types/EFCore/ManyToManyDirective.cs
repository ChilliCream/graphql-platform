using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types.EFCore
{
    public class ManyToManyDirective
    {
        // TODO: In future this could become optional, but it makes things more complicated in code generation.
        // 1. You'd need to maintain a lookup for a table combination and make sure you re-use it when you crawl the other side.
        //      e.g. Generate only AB, not AB and BA
        // 2. Would need to think about if there's tables that have multiple many-to-many relationships between themselves.
        public string JoinTable { get; set; } = default!; 

        public string ForeignKey { get; set; } = default!; // TODO: Could this be nullable? In that case it'd default to the types key, which could be OK as a default

        // TODO: Could this be nullable? I guess so.
        // You may want Order.Products but not Product --> Orders
        // I tend to think we'll want to generate this SDL very explicit, and then hiding the Orders field on product would be another directive @hidden/@internal
        // Just as we may generate the join type ProductOrders, but mark it internal, until there's further properties on it, like, addedToCartAt
        public string InverseField { get; set; } = default!; 
    }

    public class ManyToManyDirectiveType : DirectiveType<ManyToManyDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<ManyToManyDirective> descriptor)
        {
            descriptor
                .Name("manyToMany")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
               .Argument(t => t.JoinTable)
               .Description("The name of the join table to use in the database schema.")
               .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.ForeignKey)
                .Description("The name of the field to use for the foreign key in this relationship.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Argument(t => t.InverseField)
                .Description("The name of the field that navigates back to the current type.")
                .Type<NonNullType<StringType>>();
        }
    }
}
