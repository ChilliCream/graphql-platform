using System;

namespace HotChocolate.Stitching.Schemas.Contracts;

public class SomeOtherContract : IContract
{
    public string Id { get; set; } = default!;

    public string CustomerId { get; set; } = default!;

    public DateTime ExpiryDate { get; set; }
}
