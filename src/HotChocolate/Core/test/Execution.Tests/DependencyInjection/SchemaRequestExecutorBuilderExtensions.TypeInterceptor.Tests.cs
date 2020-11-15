using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.DependencyInjection
{
    public class SchemaRequestExecutorBuilderExtensionsTypeInterceptorTests
    {
        [Fact]
        public async Task OnBeforeRegisterDependencies()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

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
            var found = false;

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
            var found = false;
            var canHandleInvoked = false;

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
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeRegisterDependencies_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

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
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterRegisterDependencies()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterRegisterDependencies(
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
        public async Task OnAfterRegisterDependencies_Generic()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterRegisterDependencies<ObjectTypeDefinition>(
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
        public async Task OnAfterRegisterDependencies_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterRegisterDependencies(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterRegisterDependencies_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterRegisterDependencies<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeCompleteName()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteName(
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
        public async Task OnBeforeCompleteName_Generic()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteName<ObjectTypeDefinition>(
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
        public async Task OnBeforeCompleteName_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteName(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeCompleteName_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteName<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterCompleteName()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteName(
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
        public async Task OnAfterCompleteName_Generic()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteName<ObjectTypeDefinition>(
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
        public async Task OnAfterCompleteName_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteName(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterCompleteName_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteName<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeCompleteType()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteType(
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
        public async Task OnBeforeCompleteType_Generic()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteType<ObjectTypeDefinition>(
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
        public async Task OnBeforeCompleteType_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteType(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnBeforeCompleteType_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteType<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterCompleteType()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteType(
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
        public async Task OnAfterCompleteType_Generic()
        {
            // arrange
            Snapshot.FullName();
            var found = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteType<ObjectTypeDefinition>(
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
        public async Task OnAfterCompleteType_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnAfterCompleteType(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnAfterCompleteType_Generic_CanHandle()
        {
            // arrange
            Snapshot.FullName();
            var found = false;
            var canHandleInvoked = false;

            // act
            await CreateSchemaAsync(c => c
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("foo")
                    .Resolver("bar"))
                .OnBeforeCompleteType<ObjectTypeDefinition>(
                    (ctx, def, state) =>
                    {
                        if (def is { } && def.Name.Equals("Query"))
                        {
                            found = true;
                        }
                    },
                    c => canHandleInvoked = true));

            // assert
            Assert.True(found);
            Assert.True(canHandleInvoked);
        }

        [Fact]
        public async Task OnSchemaError()
        {
            // arrange
            Snapshot.FullName();
            Exception ex = null;

            // act
            try
            {
                await CreateSchemaAsync(
                    c => c.OnSchemaError((_, exception) => ex = exception));
            }
            catch
            {
                // ignored
            }

            // assert
            Assert.IsType<SchemaException>(ex);
        }

        [Fact]
        public void OnSchemaError_Builder_IsNull()
        {
            void Action() =>
                SchemaRequestExecutorBuilderExtensions
                    .OnSchemaError(null!, (context, exception) => { });

            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void OnSchemaError_OnError_IsNull()
        {
            var builder = new Mock<IRequestExecutorBuilder>();

            void Action() =>
                builder.Object.OnSchemaError(null!);

            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
