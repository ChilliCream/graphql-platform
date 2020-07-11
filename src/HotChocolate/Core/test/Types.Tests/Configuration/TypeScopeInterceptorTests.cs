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
            SchemaBuilder.New()
                .AddQueryType<Foo>()
                .AddTypeInterceptor(new TypeScopeInterceptor())
                .Create()
                .Print()
                .MatchSnapshot();
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