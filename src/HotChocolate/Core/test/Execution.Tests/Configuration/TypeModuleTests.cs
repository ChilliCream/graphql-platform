using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Execution.Configuration
{
    public class TypeModuleTests
    {
        [Fact]
        public async Task Use_Type_Module_From_DI()
        {
            await new ServiceCollection()
                .AddSingleton<DummyTypeModule>()
                .AddGraphQLServer()
                .AddTypeModule<DummyTypeModule>()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task Use_Type_Module_From_Factory()
        {
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddTypeModule(_ => new DummyTypeModule())
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        public class DummyTypeModule : ITypeModule
        {
            public event EventHandler<EventArgs> TypesChanged;

            public ValueTask<IReadOnlyCollection<INamedType>> CreateTypesAsync(
                IDescriptorContext context,
                CancellationToken cancellationToken)
            {
                var list = new List<INamedType>();
                var typeDefinition = new ObjectTypeDefinition("Query");
                typeDefinition.Fields.Add(new(
                    "hello",
                    type: TypeReference.Create("String"),
                    pureResolver: _ => "world"));
                list.Add(ObjectType.CreateUnsafe(typeDefinition));
                return new(list);
            }
        }
    }
}
