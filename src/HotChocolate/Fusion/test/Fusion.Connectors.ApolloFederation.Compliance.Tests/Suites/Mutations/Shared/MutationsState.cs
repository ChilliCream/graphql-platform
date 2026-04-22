using System.Collections.Concurrent;

namespace HotChocolate.Fusion.Suites.Mutations.Shared;

/// <summary>
/// Per-gateway mutable state shared across the <c>a</c>, <c>b</c>, and
/// <c>c</c> Apollo Federation subgraphs. The audit's <c>data.ts</c>
/// module-level singletons are emulated here so cross-subgraph mutation
/// ordering and shared category lists behave identically.
/// </summary>
public sealed class MutationsState
{
    private readonly List<Product> _products = [];
    private readonly List<Category> _categories = [];
    private readonly ConcurrentDictionary<string, int> _numbers = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private bool _initialized;

    /// <summary>
    /// Returns a snapshot of the current product list.
    /// </summary>
    public IReadOnlyList<Product> GetProducts()
    {
        lock (_lock)
        {
            return [.. _products];
        }
    }

    /// <summary>
    /// Returns a snapshot of the current category list.
    /// </summary>
    public IReadOnlyList<Category> GetCategories()
    {
        lock (_lock)
        {
            return [.. _categories];
        }
    }

    /// <summary>
    /// Seeds the initial product (id <c>p1</c>) once per state instance.
    /// </summary>
    public void InitProducts()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            _products.Add(new Product("p1", "p1-name", 9.99));
            _initialized = true;
        }
    }

    /// <summary>
    /// Adds a product, returning the newly created <see cref="Product"/>.
    /// </summary>
    public Product AddProduct(string name, double price)
    {
        lock (_lock)
        {
            var product = new Product($"p-added-{_products.Count}", name, price);
            _products.Add(product);
            return product;
        }
    }

    /// <summary>
    /// Removes the product with the given id. No-op when absent.
    /// </summary>
    public void DeleteProduct(string id)
    {
        lock (_lock)
        {
            _products.RemoveAll(p => string.Equals(p.Id, id, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Adds a category. Throws when the same <paramref name="requestId"/>
    /// has already been used so the planner cannot accidentally call the
    /// shareable mutation more than once.
    /// </summary>
    public Category AddCategory(string name, string requestId)
    {
        lock (_lock)
        {
            var id = $"c-added-{requestId}";

            if (_categories.Any(c => string.Equals(c.Id, id, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Category with this requestId was already added.");
            }

            var category = new Category(id, name);
            _categories.Add(category);
            return category;
        }
    }

    /// <summary>
    /// Adds <paramref name="num"/> to the running tally for
    /// <paramref name="requestId"/> and returns the new total.
    /// </summary>
    public int AddNumber(int num, string requestId)
    {
        return _numbers.AddOrUpdate(requestId, num, (_, current) => current + num);
    }

    /// <summary>
    /// Multiplies the running tally for <paramref name="requestId"/> and
    /// returns the result.
    /// </summary>
    public int MultiplyNumber(int by, string requestId)
    {
        return _numbers.AddOrUpdate(requestId, 0, (_, current) => current * by);
    }

    /// <summary>
    /// Removes the running tally for <paramref name="requestId"/> and
    /// returns its last value (or 0 when absent).
    /// </summary>
    public int DeleteNumber(string requestId)
    {
        return _numbers.TryRemove(requestId, out var value) ? value : 0;
    }
}

/// <summary>
/// Product record shared across the mutations suite subgraphs.
/// </summary>
public sealed record Product(string Id, string Name, double Price);

/// <summary>
/// Category record shared across the mutations suite subgraphs.
/// </summary>
public sealed record Category(string Id, string Name);
