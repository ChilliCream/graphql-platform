#nullable enable

namespace HotChocolate.Types;

internal interface IHasOptional
{
    bool IsOptional { get; }
}
