using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class ContractStorage
    {
        public List<IContract> Contracts { get; } = new List<IContract>
        {
            new LifeInsuranceContract
            {
                Id = "1",
                CustomerId= "1",
                Premium = 123456.11
            },
            new LifeInsuranceContract
            {
                Id = "2",
                CustomerId= "1",
                Premium = 456789.12
            },
            new LifeInsuranceContract
            {
                Id = "3",
                CustomerId = "2",
                Premium = 789.12
            },
            new SomeOtherContract
            {
                Id = "1",
                CustomerId= "1",
                ExpiryDate = new DateTime(2015, 2, 1, 0,0,0, DateTimeKind.Utc)
            },
            new SomeOtherContract
            {
                Id = "2",
                CustomerId= "2",
                ExpiryDate = new DateTime(2015, 5, 1, 0,0,0, DateTimeKind.Utc)
            },
            new SomeOtherContract
            {
                Id = "3",
                CustomerId= "3",
                ExpiryDate = new DateTime(2017, 1, 30, 0,0,0, DateTimeKind.Utc)
            },
            new SomeOtherContract
            {
                Id = "4",
                CustomerId= "3",
                ExpiryDate = new DateTime(2020, 1, 1, 0,0,0, DateTimeKind.Utc)
            }
        };
    }
}
