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
            [Scope]
            public Bar Bar1 => new Bar();
            public Bar Bar2 => new Bar();
        }

        public class Bar
        {
            public Baz Baz => new Baz();
        }

        public class Baz
        {
            public string SomeString => "hello";
        }

        public class ScopeAttribute : ObjectFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IObjectFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor
                    .Extend()
                    .OnBeforeCreate(d =>
                    {
                        d.Type = ((ClrTypeReference)d.Type).WithScope("Scope");
                    });
            }
        }

        public class TypeScopeInterceptor
            : TypeInitializationInterceptor
            , ITypeScopeInterceptor
        {
            public override bool CanHandle(
                ITypeSystemObjectContext context) =>
                context is { Scope: "Scope" };

            public override void OnBeforeRegisterDependencies(
                ITypeDiscoveryContext discoveryContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                if (definition is ObjectTypeDefinition def)
                {
                    foreach (ObjectFieldDefinition field in def.Fields)
                    {
                        field.Type = field.Type.With(scope: discoveryContext.Scope);
                    }
                }
            }

            public override void OnBeforeCompleteName(
                ITypeCompletionContext completionContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                definition.Name = "Scope_" + definition.Name;
            }

            public bool TryCreateScope(
                ITypeDiscoveryContext discoveryContext,
                [NotNullWhen(true)] out IReadOnlyList<TypeDependency> typeDependencies)
            {
                if (discoveryContext is { Scope: "Scope" })
                {
                    typeDependencies = discoveryContext.TypeDependencies
                        .Select(t => t.With(((ClrTypeReference)t.TypeReference).WithScope("Scope")))
                        .ToList();
                    return true;
                }

                typeDependencies = null;
                return false;
            }
        }
    }
}