using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types;

[ExtendObjectType(typeof(Person))]
public class PersonAddress
{
    public Task<string> GetAddressAsync(
        AddressDataLoader dataLoader,
        CancellationToken cancellationToken)
        => dataLoader.LoadAsync("abc", cancellationToken);
}
