using System;
using System.Linq;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class CustomerResolver
    {
        public Consultant GetConsultant(
            Customer customer,
            [Service]CustomerRepository repository)
        {
            if (customer.ConsultantId != null)
            {
                return repository.Consultants.FirstOrDefault(
                    t => t.Id.Equals(customer.ConsultantId));
            }
            return null;
        }
    }
}
