using System;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class SomeOtherContract
        : IContract
    {
        public string Id { get; set; }

        public string CustomerId { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}
