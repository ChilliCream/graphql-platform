using HotChocolate.Features;
using HotChocolate.Language;

namespace HotChocolate.Types;

public sealed class IObjectTypeDefinitionTests
{
    [Fact]
    public void IsDeprecated_Should_DefaultToFalse_When_ImplementerDoesNotOverrideIt()
    {
        // arrange
        IObjectTypeDefinition objectType = new MinimalObjectTypeDefinition();

        // act
        var isDeprecated = objectType.IsDeprecated;

        // assert
        Assert.False(isDeprecated);
        Assert.Null(objectType.DeprecationReason);
    }

    private sealed class MinimalObjectTypeDefinition : IObjectTypeDefinition
    {
        public string Name => "Minimal";

        public string? Description => null;

        public TypeKind Kind => TypeKind.Object;

        public IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> Fields
            => throw new NotSupportedException();

        public IReadOnlyInterfaceTypeDefinitionCollection Implements
            => throw new NotSupportedException();

        public IReadOnlyDirectiveCollection Directives => throw new NotSupportedException();

        public IFeatureCollection Features => throw new NotSupportedException();

        public Type RuntimeType => typeof(object);

        public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

        public bool IsAssignableFrom(ITypeDefinition type) => throw new NotSupportedException();

        public bool IsImplementing(string typeName) => throw new NotSupportedException();

        public bool IsImplementing(IInterfaceTypeDefinition interfaceType)
            => throw new NotSupportedException();

        public bool Equals(IType? other) => throw new NotSupportedException();

        public bool Equals(IType? other, TypeComparison comparison)
            => throw new NotSupportedException();

        public ObjectTypeDefinitionNode ToSyntaxNode() => throw new NotSupportedException();

        ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
    }
}
