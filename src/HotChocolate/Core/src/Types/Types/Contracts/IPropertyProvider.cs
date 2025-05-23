#nullable enable

using System.Reflection;

namespace HotChocolate.Types;

internal interface IPropertyProvider
{
    PropertyInfo? Property { get; }
}
