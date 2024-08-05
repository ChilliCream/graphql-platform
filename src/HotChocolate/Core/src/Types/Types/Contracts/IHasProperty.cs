#nullable enable

using System.Reflection;

namespace HotChocolate.Types;

internal interface IHasProperty
{
    PropertyInfo? Property { get; }
}
