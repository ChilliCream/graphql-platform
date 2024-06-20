using HotChocolate.Types;

namespace HotChocolate.Fusion.Shared;

[DirectiveType("null", DirectiveLocation.FieldDefinition)]
// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class NullDirective
{
    public int? AtIndex { get; set; }
}
