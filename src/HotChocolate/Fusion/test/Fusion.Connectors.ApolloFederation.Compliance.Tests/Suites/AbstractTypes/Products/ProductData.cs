using HotChocolate.Fusion.Suites.AbstractTypes.Agency;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public static class ProductData
{
    public static readonly IReadOnlyList<UserRef> Users =
    [
        new UserRef { InternalId = "u1", Email = "u1@example.com" },
        new UserRef { InternalId = "u2", Email = "u2@example.com" },
        new UserRef { InternalId = "u3", Email = "u3@example.com" }
    ];

    public static readonly IReadOnlyDictionary<string, UserRef> UsersByInternalId =
        Users.ToDictionary(static u => u.InternalId, StringComparer.Ordinal);

    public static readonly IReadOnlyList<BookEntity> Books =
    [
        new BookEntity
        {
            Id = "p1",
            Sku = "sku-1",
            Dimensions = new ProductDimensionValue { Size = "small", Weight = 0.5 },
            CreatedByInternalId = "u1",
            Publisher = new SelfPublisher { Email = "u1@example.com" },
            Hidden = false
        },
        new BookEntity
        {
            Id = "p3",
            Sku = "sku-3",
            Dimensions = new ProductDimensionValue { Size = "small", Weight = 0.6 },
            CreatedByInternalId = "u2",
            Publisher = new AgencyPublisher { Id = "a1" },
            Hidden = false
        }
    ];

    public static readonly IReadOnlyDictionary<string, BookEntity> BooksById =
        Books.ToDictionary(static b => b.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<MagazineEntity> Magazines =
    [
        new MagazineEntity
        {
            Id = "p2",
            Sku = "sku-2",
            Dimensions = new ProductDimensionValue { Size = "small", Weight = 0.2 },
            CreatedByInternalId = "u1",
            Publisher = new AgencyPublisher { Id = "a1" },
            Hidden = false
        },
        new MagazineEntity
        {
            Id = "p4",
            Sku = "sku-4",
            Dimensions = new ProductDimensionValue { Size = "small", Weight = 0.3 },
            CreatedByInternalId = "u2",
            Publisher = new SelfPublisher { Email = "u1@example.com" },
            Hidden = true
        }
    ];

    public static readonly IReadOnlyDictionary<string, MagazineEntity> MagazinesById =
        Magazines.ToDictionary(static m => m.Id, StringComparer.Ordinal);

    public static readonly IReadOnlyList<IProductEntity> AllProducts =
        Books.Cast<IProductEntity>().Concat(Magazines).ToList();

    public static readonly IReadOnlyDictionary<string, IProductEntity> AllProductsById =
        AllProducts.ToDictionary(static p => p.Id, StringComparer.Ordinal);

    public static int CountProductsByUser(string internalId)
        => AllProducts.Count(p => p.CreatedByInternalId == internalId);

    public static UserRef? ResolveCreatedBy(string createdByInternalId)
        => UsersByInternalId.TryGetValue(createdByInternalId, out var user) ? user : null;
}

public interface IProductEntity
{
    string Id { get; }
    string? Sku { get; }
    ProductDimensionValue? Dimensions { get; }
    string CreatedByInternalId { get; }
    IPublisher? Publisher { get; }
    bool Hidden { get; }
    string TypeName { get; }
}

public sealed class BookEntity : IProductEntity
{
    public string Id { get; init; } = default!;
    public string? Sku { get; init; }
    public ProductDimensionValue? Dimensions { get; init; }
    public string CreatedByInternalId { get; init; } = default!;
    public IPublisher? Publisher { get; init; }
    public bool Hidden { get; init; }
    public string TypeName => "Book";
}

public sealed class MagazineEntity : IProductEntity
{
    public string Id { get; init; } = default!;
    public string? Sku { get; init; }
    public ProductDimensionValue? Dimensions { get; init; }
    public string CreatedByInternalId { get; init; } = default!;
    public IPublisher? Publisher { get; init; }
    public bool Hidden { get; init; }
    public string TypeName => "Magazine";
}

public sealed class ProductDimensionValue
{
    public string? Size { get; init; }
    public double? Weight { get; init; }
}

public sealed class UserRef
{
    public string InternalId { get; init; } = default!;
    public string Email { get; init; } = default!;
}

public interface IPublisher;

public sealed class AgencyPublisher : IPublisher
{
    public string Id { get; init; } = default!;
}

public sealed class SelfPublisher : IPublisher
{
    public string Email { get; init; } = default!;
}
