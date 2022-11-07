using System;
using System.Linq;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class Query
    {
        private readonly IdSerializer _idSerializer = new IdSerializer();
        private readonly CustomerRepository _repository;

        public Query(CustomerRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public Customer GetCustomer(string id)
        {
            IdValue value = _idSerializer.Deserialize(id);
            return _repository.Customers
                .FirstOrDefault(t => t.Id.Equals(value.Value));
        }

        public Customer[] GetCustomers(string[] ids)
        {
            var customers = new Customer[ids.Length];

            for(int i = 0; i < ids.Length; i++)
            {
                customers[i] = GetCustomer(ids[i]);
            }

            return customers;
        }

        public Customer[] GetAllCustomers() =>
            _repository.Customers.ToArray();

        public Consultant GetConsultant(string id)
        {
            IdValue value = _idSerializer.Deserialize(id);
            return _repository.Consultants
                .FirstOrDefault(t => t.Id.Equals(value.Value));
        }

        public ICustomerOrConsultant GetCustomerOrConsultant(string id)
        {
            IdValue value = _idSerializer.Deserialize(id);
            if (value.TypeName == "Consultant")
            {
                return GetConsultant(id);
            }
            return GetCustomer(id);
        }

        public Customer GetCustomer(CustomerKind kind)
        {
            return _repository.Customers.FirstOrDefault(t => t.Kind == kind);
        }
    }
}
