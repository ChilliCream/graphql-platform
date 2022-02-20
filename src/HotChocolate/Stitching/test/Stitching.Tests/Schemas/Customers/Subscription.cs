using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers;

public class Subscription
{
    public async IAsyncEnumerable<Customer> OnCustomerChangedStreamAsync()
    {
        await Task.Delay(1);
        yield return new Customer { Name = "abc" };
        yield return new Customer { Name = "def" };
    }

    [Subscribe(With = nameof(OnCustomerChangedStreamAsync))]
    public Customer OnCustomerChanged([EventMessage] Customer customer) => customer;
}
