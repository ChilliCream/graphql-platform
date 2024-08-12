using HotChocolate.Types;

namespace HotChocolate.Fusion.Shared;

[DirectiveType("error", DirectiveLocation.FieldDefinition)]
// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class ErrorDirective
{
    public int? AtIndex { get; set; }
}
