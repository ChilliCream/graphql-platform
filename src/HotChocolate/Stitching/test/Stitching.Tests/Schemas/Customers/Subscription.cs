using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers;

public class Subscription
{
    public async IAsyncEnumerable<Customer> OnCustomerChangedStreamAsync()
    {
        await Task.Delay(1);
        yield return new Customer { Id = "1", Name = "abc" };
        yield return new Customer { Id = "2", Name = "def" };
    }

    [Subscribe(With = nameof(OnCustomerChangedStreamAsync))]
    public Customer OnCustomerChanged([EventMessage] Customer customer) => customer;
}
