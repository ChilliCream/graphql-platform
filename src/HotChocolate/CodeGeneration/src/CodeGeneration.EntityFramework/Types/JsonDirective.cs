using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class JsonDirective
    {

    }

    public class JsonDirectiveType : DirectiveType<JsonDirective>
    {
        public const string NameConst = "json";

        protected override void Configure(IDirectiveTypeDescriptor<JsonDirective> descriptor)
        {
            descriptor
                .Name(NameConst)

                // TODO: What does this do exactly? Instruct that it's a nested document on a record in a JSON typed col?
                // The example we have is actually quite interesting too given that it's plural (Customer.ShipppingAddresses)
                //.Description("...")

                // TODO: Would we want to support this on a field too?
                // In fact, perhaps on a field is the only way we'd want to support it.

                // Docs on how this is supported in npgsql:
                // https://www.npgsql.org/efcore/mapping/json.html
                /*
                 * 
                 * type Customer {
                 *  ShippingAddresses: [Address!]! @json // nested in col
                 * }
                 * 
                 * type Address { ... }
                 * 
                 * type Store {
                 *  Address: Address! @manyToOne(...) // e.g. link to a real table
                 * }
                 */
                .Location(DirectiveLocation.Object);

        }
    }
}
