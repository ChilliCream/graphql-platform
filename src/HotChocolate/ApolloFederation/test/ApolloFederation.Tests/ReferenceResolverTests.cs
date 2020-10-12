using System.Threading.Tasks;

namespace HotChocolate.ApolloFederation
{
    public class ReferenceResolverTests
    {

        [ForeignServiceTypeExtension]
        [ReferenceResolver(EntityResolverType = typeof(FooRefResolver))]
        public class FooWithResolverClass
        {
            [Key] public string Some1 { get; set; }
            [Key] public string Some2 { get; set; }
        }

        [ForeignServiceTypeExtension]
        [ReferenceResolver(EntityResolver = nameof(GetAsync))]
        public class FooWithInClassReferenceResolver
        {
            [Key] public string Some1 { get; set; }
            [Key] public string Some2 { get; set; }

            public Task<FooWithInClassReferenceResolver> GetAsync(string some1, string some2)
            {
                // some code ....
                return Task.FromResult(new FooWithInClassReferenceResolver());
            }
        }

        class FooRefResolver
        {
            public Task<FooWithResolverClass> GetAsync(string some1, string some2)
            {
                // some code ....
                return Task.FromResult(new FooWithResolverClass());
            }
        }
    }
}
