using HotChocolate.Types;

namespace HotChocolate.Fusion;

[DirectiveType("error", DirectiveLocation.FieldDefinition)]
// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class ErrorDirective
{
    public int? AtIndex { get; set; }
}
