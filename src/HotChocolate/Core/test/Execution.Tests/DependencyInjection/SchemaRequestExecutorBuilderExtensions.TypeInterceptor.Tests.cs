using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.DependencyInjection
{
    public class SchemaRequestExecutorBuilderExtensions_TypeInterceptorTests
    {
        [Fact]
        public async Task OnBeforeRegisterDependencies()
        {
            // arrange
            Snapshot.FullName();
            bool found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeRegisterDependencies(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    }));

            // assert
            Assert.True(found);
        }

        [Fact]
        public async Task OnBeforeRegisterDependencies_Generic()
        {
            // arrange
            Snapshot.FullName();
            bool found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeRegisterDependencies<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    }));

            // assert
            Assert.True(found);
        }

        [Fact]
        public async Task OnBeforeRegisterDependencies_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            bool found = false;
            bool canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeRegisterDependencies(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    canHandle: c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeRegisterDependencies_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            bool found = false;
            bool canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeRegisterDependencies<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    canHandle: c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }
    }
}
