namespace HotChocolate.Stitching.Schemas.Contracts;

public class LifeInsuranceContract : IContract
{
    public string Id { get; set; } = default!;

    public string CustomerId { get; set; } = default!;

    public double Premium { get; set; }
}
