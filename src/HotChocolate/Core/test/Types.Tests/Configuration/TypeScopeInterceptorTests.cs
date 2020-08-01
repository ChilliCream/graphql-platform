using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeScopeInterceptorTests
    {
        [Fact]
        public void BranchTypesWithScope()
        {
            var types = new List<ITypeSystemMember>();

            SchemaBuilder.New()
                .AddQueryType<Foo>()
                .AddTypeInterceptor(new TypeScopeInterceptor(types))
                .Create()
                .Print()
                .MatchSnapshot();

            Assert.Collection(
                types.OfType<INamedType>().Select(t => t.Name).OrderBy(t => t),
                name => Assert.Equal("A_Bar", name),
                name => Assert.Equal("B_Bar", name),
                name => Assert.Equal("C_Baz", name));
        }

        [Fact]
        public void Scalars_Should_BeEqual_WhenDifferentScope()
        {
            var types = new List<ITypeSystemMember>();

            SchemaBuilder.New()
                .AddQueryType<FooScalarType>()
                .AddTypeInterceptor(new TypeScopeInterceptor(types))
                .Create()
                .Print()
                .MatchSnapshot();
        }

        public class FooScalarType : ObjectType<FooScalar>
        {
            protected override void Configure(IObjectTypeDescriptor<FooScalar> descriptor)
            {
                descriptor.Field("Bar3").Resolver("").Type(new StringType())
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Type = ((SchemaTypeReference)d.Type).WithScope("Bar3");
                    });

                descriptor.Field(x => x.Bar1)
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Type = ((ClrTypeReference)d.Type).WithScope("Bar1");
                    });

                descriptor.Field(x => x.Bar1)
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Type = ((ClrTypeReference)d.Type).WithScope("Bar2");
                    });
            }
        }

        public class FooScalar
        {
            public string Bar1 => "";

            public string Bar2 => "";
        }

        public class Foo
        {
            [Scope(Scope = "A")]
            public Bar Bar1 => new Bar();

            [Scope(Scope = "B")]
            public Bar Bar2 => new Bar();
        }

        public class Bar
        {
            [Scope(Scope = "C")]
            public Baz Baz => new Baz();
        }

        public class Baz
        {
            public string SomeString => "hello";
        }

        public class ScopeAttribute : ObjectFieldDescriptorAttribute
        {
            public string Scope { get; set; }

            public override void OnConfigure(
                IDescriptorContext context,
                IObjectFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Type = ((ClrTypeReference)d.Type).WithScope(Scope);
                    });
            }
        }

        public class TypeScopeInterceptor
            : TypeInterceptor
            , ITypeScopeInterceptor
        {
            private readonly ICollection<ITypeSystemMember> _types;

            public TypeScopeInterceptor(ICollection<ITypeSystemMember> types)
            {
                _types = types;
            }

            public override bool TriggerAggregations => true;

            public override bool CanHandle(
                ITypeSystemObjectContext context) =>
                context is { Scope: { } };

            public override void OnBeforeRegisterDependencies(
                ITypeDiscoveryContext discoveryContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                if (definition is ObjectTypeDefinition def)
                {
                    foreach (ObjectFieldDefinition field in def.Fields)
                    {
                        if (field.Type.Scope is null)
                        {
                            field.Type = field.Type.With(scope: discoveryContext.Scope);
                        }
                    }
                }
            }

            public override void OnBeforeCompleteName(
                ITypeCompletionContext completionContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                definition.Name = completionContext.Scope + "_" + definition.Name;
            }

            public override void OnTypesInitialized(
                IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
            {
                foreach (ITypeDiscoveryContext context in discoveryContexts)
                {
                    _types.Add(context.Type);
                }
            }

            public bool TryCreateScope(
                ITypeDiscoveryContext discoveryContext,
                [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies)
            {
                if (discoveryContext is { Scope: { } })
                {
                    typeDependencies = discoveryContext.TypeDependencies
                        .Where(t => t.TypeReference.Scope is null)
                        .Select(t => t
                            .With(((ClrTypeReference)t.TypeReference)
                            .WithScope(discoveryContext.Scope)))
                        .ToList();
                    return true;
                }

                typeDependencies = null;
                return false;
            }
        }
    }
}