using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed record NodeLookupWorkItem(
    Lookup? Lookup,
    string ResponseName,
    IValueNode IdArgumentValue,
    SelectionSet SelectionSet) : WorkItem;
