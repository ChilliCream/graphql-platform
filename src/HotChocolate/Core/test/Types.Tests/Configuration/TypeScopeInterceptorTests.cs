using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Configuration;

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
            .ToString()
            .MatchSnapshot();

        Assert.Collection(
            types.OfType<ITypeDefinition>().Select(t => t.Name).OrderBy(t => t),
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

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(d => d.Type = ((ExtendedTypeReference)d.Type).WithScope(Scope));
        }
    }

    public class TypeScopeInterceptor : TypeInterceptor
    {
        private readonly ICollection<ITypeSystemMember> _types;
        private readonly List<ITypeDiscoveryContext> _contexts = [];

        public TypeScopeInterceptor(ICollection<ITypeSystemMember> types)
        {
            _types = types;
        }

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            TypeSystemConfiguration configuration)
        {
            if (discoveryContext is { Scope: { } } && configuration is ObjectTypeConfiguration def)
            {
                _contexts.Add(discoveryContext);

                foreach (var field in def.Fields)
                {
                    if (field.Type is not null && field.Type.Scope is null)
                    {
                        field.Type = field.Type.With(scope: discoveryContext.Scope);
                    }
                }
            }
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            TypeSystemConfiguration configuration)
        {
            if (completionContext is { Scope: { } })
            {
                configuration.Name = completionContext.Scope + "_" + configuration.Name;
            }
        }

        public override void OnTypesInitialized()
        {
            foreach (var context in _contexts)
            {
                _types.Add(context.Type);
            }
        }
    }
}
