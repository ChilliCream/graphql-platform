using System;
using System.Threading.Tasks;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class DependencyInjectionTests
    {
        [Fact]
        public async Task Extension_With_Constructor_Injection()
        {
            var fullName = Snapshot.FullName();

            // this test ensures that we inject services into type instances without the need of
            // registering the type into the dependency container.
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton<SomeService>()
                    .AddGraphQL()
                    .AddQueryType<Query1>()
                    .AddType<ExtendQuery1>()
                    .BuildRequestExecutorAsync();
            
            await executor
                .ExecuteAsync("{ hello }")
                .MatchSnapshotAsync(fullName, "1");

            await executor
                .ExecuteAsync("{ hello }")
                .MatchSnapshotAsync(fullName, "2");
        }

        [Fact]
        public async Task Extension_With_Scoped_Constructor_Injection()
        {
            Snapshot.FullName();

            IServiceProvider services =
                new ServiceCollection()
                    .AddScoped<SomeService>()
                    .AddScoped<ExtendQuery1>()
                    .AddGraphQL()
                    .AddQueryType<Query1>()
                    .AddType<ExtendQuery1>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor = await services.GetRequestExecutorAsync();

            using (IServiceScope scope = services.CreateScope())
            {
                await executor
                    .ExecuteAsync(
                        QueryRequestBuilder
                            .New()
                            .SetQuery("{ hello }")
                            .SetServices(scope.ServiceProvider)
                            .Create())
                    .MatchSnapshotAsync();
            }

            using (IServiceScope scope = services.CreateScope())
            {
                await executor
                    .ExecuteAsync(
                        QueryRequestBuilder
                            .New()
                            .SetQuery("{ hello }")
                            .SetServices(scope.ServiceProvider)
                            .Create())
                    .MatchSnapshotAsync();
            }
        }

        [Fact]
        public async Task Type_With_Constructor_Injection()
        {
            var name = Snapshot.FullName();

            // this test ensures that we inject services into type instances without the need of
            // registering the type into the dependency container.
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddSingleton<SomeService>()
                    .AddGraphQL()
                    .AddQueryType<Query2>()
                    .BuildRequestExecutorAsync();

            await executor
                .ExecuteAsync("{ hello }")
                .MatchSnapshotAsync(name, "1");

            await executor
                .ExecuteAsync("{ hello }")
                .MatchSnapshotAsync(name, "2");
        }

        [Fact]
        public async Task Type_With_Scoped_Constructor_Injection()
        {
            Snapshot.FullName();

            IServiceProvider services =
                new ServiceCollection()
                    .AddScoped<SomeService>()
                    .AddScoped<Query2>()
                    .AddGraphQL()
                    .AddQueryType<Query2>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor = await services.GetRequestExecutorAsync();

            using (IServiceScope scope = services.CreateScope())
            {
                await executor
                    .ExecuteAsync(
                        QueryRequestBuilder
                            .New()
                            .SetQuery("{ hello }")
                            .SetServices(scope.ServiceProvider)
                            .Create())
                    .MatchSnapshotAsync();
            }

            using (IServiceScope scope = services.CreateScope())
            {
                await executor
                    .ExecuteAsync(
                        QueryRequestBuilder
                            .New()
                            .SetQuery("{ hello }")
                            .SetServices(scope.ServiceProvider)
                            .Create())
                    .MatchSnapshotAsync();
            }
        }

        public class SomeService
        {
            private int _i;

            public int Count => _i;

            public string SayHello() => "Hello_" + _i++;
        }

        public class Query1
        {

        }

        [ExtendObjectType(typeof(Query1))]
        public class ExtendQuery1
        {
            private readonly SomeService _service;

            public ExtendQuery1(SomeService service)
            {
                _service = service ?? throw new ArgumentNullException(nameof(service));
            }

            public string Hello() => _service.SayHello();
        }

        public class Query2
        {
            private readonly SomeService _service;

            public Query2(SomeService service)
            {
                _service = service ?? throw new ArgumentNullException(nameof(service));
            }

            public string Hello() => _service.SayHello();
        }
    }
}
