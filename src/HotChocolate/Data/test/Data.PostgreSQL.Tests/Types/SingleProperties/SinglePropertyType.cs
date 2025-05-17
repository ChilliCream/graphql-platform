using HotChocolate.Data.Models;
using HotChocolate.Types;

namespace HotChocolate.Data.Types.SingleProperties;

[ObjectType<SingleProperty>]
public static partial class SinglePropertyType
{
    public static string GetId(
        [Parent(requires: nameof(singleProperty.Id))]
        SingleProperty singleProperty)
        => singleProperty.Id;
}
