using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class Query
    {
        private readonly IdSerializer _idSerializer = new IdSerializer();
        private readonly ContractStorage _contractStorage;

        public Query(ContractStorage contractStorage)
        {
            _contractStorage = contractStorage
                ?? throw new ArgumentNullException(nameof(contractStorage));
        }

        public IContract GetContract(string contractId)
        {
            IdValue value = _idSerializer.Deserialize(contractId);

            if (value.TypeName == nameof(LifeInsuranceContract))
            {
                return _contractStorage.Contracts
                    .OfType<LifeInsuranceContract>()
                    .FirstOrDefault(t => t.Id.Equals(value.Value));
            }
            else
            {
                return _contractStorage.Contracts
                    .OfType<SomeOtherContract>()
                    .FirstOrDefault(t => t.Id.Equals(value.Value));
            }
        }

        public IEnumerable<IContract> GetContracts(string customerId)
        {
            IdValue value = _idSerializer.Deserialize(customerId);

            return _contractStorage.Contracts
                .Where(t => t.CustomerId.Equals(value.Value));
        }

        public int GetInt(int i)
        {
            return i;
        }

        public Guid GetGuid(Guid guid)
        {
            return guid;
        }
    }
}
