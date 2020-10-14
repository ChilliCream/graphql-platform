using System.Collections.Generic;
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
                .TryAddTypeInterceptor(new TypeScopeInterceptor(types))
                .Create()
                .Print()
                .MatchSnapshot();

            Assert.Collection(
                types.OfType<INamedType>().Select(t => t.Name).OrderBy(t => t),
                name => Assert.Equal("A_Bar", name),
                name => Assert.Equal("B_Bar", name),
                name => Assert.Equal("C_Baz", name));
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

            [Scope(Scope = "D")]
            public string SomeString => "hello";
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
                        d.Type = ((ExtendedTypeReference)d.Type).WithScope(Scope);
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
                out IReadOnlyList<TypeDependency> typeDependencies)
            {
                if (discoveryContext is { Scope: not null })
                {
                    var list = new List<TypeDependency>();

                    foreach (TypeDependency typeDependency in discoveryContext.TypeDependencies)
                    {
                        if (!discoveryContext.TryPredictTypeKind(
                            typeDependency.TypeReference,
                            out TypeKind kind) ||
                            kind == TypeKind.Scalar)
                        {
                            list.Add(typeDependency);
                            continue;
                        }

                        var typeReference = (ExtendedTypeReference)typeDependency.TypeReference;

                        if (typeDependency.TypeReference.Scope is null)
                        {
                            typeReference = typeReference.WithScope(discoveryContext.Scope);
                            list.Add(typeDependency.With(typeReference));
                        }
                        else
                        {
                            list.Add(typeDependency);
                        }

                        typeDependencies = list;
                        return true;
                    }
                }

                typeDependencies = null;
                return false;
            }
        }
    }
}
