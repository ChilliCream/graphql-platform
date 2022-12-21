#nullable enable
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public sealed class DirectiveArgument : Argument, IHasProperty
{
    public DirectiveArgument(DirectiveArgumentDefinition definition, int index)
        : base(definition, index)
    {
        Property = definition.Property;
    }

    public PropertyInfo? Property { get; }
}
