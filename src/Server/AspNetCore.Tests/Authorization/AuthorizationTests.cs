using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class AuthorizationTests
        : IClassFixture<TestServerFactory>
    {
        public AuthorizationTests(TestServerFactory testServerFactory)
        {
            TestServerFactory = testServerFactory;
        }


        private TestServerFactory TestServerFactory { get; }




    }

    public class MinimumAgeRequirement
        : IAuthorizationRequirement
    {
        public int MinimumAge { get; private set; }

        public MinimumAgeRequirement(int minimumAge)
        {
            MinimumAge = minimumAge;
        }
    }
}
