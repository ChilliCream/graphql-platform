using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.OpenApi.Tests.Controller;

public record Activities(int Offset, int Limit, int Count, Activity[] History);

public record Activity(string Uuid);

public record Profile(
    [property: JsonPropertyName("first_name")]
    string FirstName,
    [property: JsonPropertyName("last_name")]
    string LastName,
    string Email,
    string Picture,
    [property: JsonPropertyName("promo_code")]
    string PromoCode
);

public record PriceEstimate(
    [property: JsonPropertyName("product_id")]
    string ProductId,
    [property: JsonPropertyName("currency_code")]
    string CurrencyCode,
    [property: JsonPropertyName("display_name")]
    string DisplayName,
    string Estimate,
    [property: JsonPropertyName("low_estimate")]
    float LowEstimate,
    [property: JsonPropertyName("high_estimate")]
    float HighEstimate,
    [property: JsonPropertyName("surge_multiplier")]
    float SurgeMultiplier
);

public record Product(
    [property: JsonPropertyName("product_id")]
    string ProductId,
    string Description,
    [property: JsonPropertyName("display_name")]
    string DisplayName,
    string Capacity,
    string Image
);

[ApiController]
public class UberController
{
    private static readonly List<Product> _products =
    [
        new Product("ProductA", "Desc", "Product A", "Cap", "http://img.png"),
        new Product("ProductB", "Desc", "Product B", "Cap", "http://img.png"),
    ];

    [HttpGet]
    [Route("/products")]
    public IActionResult GetProducts([Required] double longitude, [Required] double latitude) =>
        new ObjectResult(_products);

    [HttpGet]
    [Route("/me")]
    public IActionResult GetMe() => new ObjectResult(new Profile(
        "Max",
        "Mustermann",
        "max.mustermann@email.abc",
        "http://img.png",
        "1234"
    ));
}
