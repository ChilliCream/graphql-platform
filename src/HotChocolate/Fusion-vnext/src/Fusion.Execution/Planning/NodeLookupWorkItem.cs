using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed record NodeLookupWorkItem(
    Lookup? Lookup,
    string ResponseName,
    IValueNode IdArgumentValue,
    SelectionSet SelectionSet) : WorkItem;
