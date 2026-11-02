using HotChocolate.Fusion.Planning.Partitioners;

namespace HotChocolate.Fusion.Planning;

internal sealed record NodeFieldWorkItem(NodeField NodeField) : WorkItem;
