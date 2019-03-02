using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class Mutation
    {
        private readonly CustomerRepository _repository;

        public Mutation(CustomerRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public CreateCustomerPayload CreateCustomer(CreateCustomerInput input)
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid().ToString(),
                Name = input.Name,
                Street = input.Street
            };

            _repository.Customers.Add(customer);

            return new CreateCustomerPayload { Customer = customer };
        }

        public ICollection<CreateCustomerPayload> CreateCustomers(
            ICollection<CreateCustomerInput> inputs)
        {
            var results = new List<CreateCustomerPayload>();

            foreach (CreateCustomerInput input in inputs)
            {
                results.Add(CreateCustomer(input));
            }

            return results;
        }
    }
}
