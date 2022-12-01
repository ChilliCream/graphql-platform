using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Schemas.Customers;

public class Query
{
    private readonly IdSerializer _idSerializer = new();
    private readonly CustomerRepository _repository;

    public Query(CustomerRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public Customer? GetCustomer(string id)
    {
        var value = _idSerializer.Deserialize(id);
        return _repository.Customers.Find(t => t.Id?.Equals(value.Value) ?? false);
    }

    public Customer[] GetCustomers(string[] ids)
    {
        var customers = new Customer[ids.Length];

        for(var i = 0; i < ids.Length; i++)
        {
            customers[i] = GetCustomer(ids[i])!;
        }

        return customers;
    }

    public Customer[] GetAllCustomers() =>
        _repository.Customers.ToArray();

    public Consultant? GetConsultant(string id)
    {
        var value = _idSerializer.Deserialize(id);
        return _repository.Consultants.Find(t => t.Id?.Equals(value.Value) ?? false);
    }

    public ICustomerOrConsultant? GetCustomerOrConsultant(string id)
    {
        var value = _idSerializer.Deserialize(id);
        return value.TypeName == "Consultant" ? GetConsultant(id) : GetCustomer(id);
    }

    public Customer? GetCustomer(CustomerKind kind)
    => _repository.Customers.Find(t => t.Kind == kind);
}
