using System.Threading.Tasks;

namespace HotChocolate.ApolloFederation;

public class ReferenceResolverTests
{
    [ForeignServiceTypeExtension]
    [ReferenceResolver(EntityResolverType = typeof(FooRefResolver))]
    public class FooWithResolverClass
    {
        [Key] public string Some1 { get; set; } = default!;
        [Key] public string Some2 { get; set; } = default!;
    }

    [ForeignServiceTypeExtension]
    [ReferenceResolver(EntityResolver = nameof(GetAsync))]
    public class FooWithInClassReferenceResolver
    {
        [Key] public string Some1 { get; set; } = default!;
        [Key] public string Some2 { get; set; } = default!;

        public Task<FooWithInClassReferenceResolver> GetAsync(string some1, string some2)
        {
            // some code ....
            return Task.FromResult(new FooWithInClassReferenceResolver());
        }
    }

    public class FooRefResolver
    {
        public Task<FooWithResolverClass> GetAsync(string some1, string some2)
        {
            // some code ....
            return Task.FromResult(new FooWithResolverClass());
        }
    }
}
