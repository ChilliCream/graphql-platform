using HotChocolate.Types;

namespace HotChocolate.Fusion;

[DirectiveType("returns", DirectiveLocation.FieldDefinition)]
// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class ReturnsDirective
{
    public List<string> Types { get; set; } = [];
}
