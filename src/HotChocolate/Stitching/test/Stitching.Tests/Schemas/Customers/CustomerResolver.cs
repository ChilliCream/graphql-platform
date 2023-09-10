namespace HotChocolate.Stitching.Schemas.Customers;

public class CustomerResolver
{
    public Consultant? GetConsultant(
        [Parent] Customer customer,
        [Service] CustomerRepository repository)
    {
        if (customer.ConsultantId != null)
        {
            return repository.Consultants.Find(t => t.Id?.Equals(customer.ConsultantId) ?? false);
        }

        return null;
    }
}
