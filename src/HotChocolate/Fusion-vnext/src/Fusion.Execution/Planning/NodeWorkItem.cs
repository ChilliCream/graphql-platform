using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed record NodeWorkItem(FieldNode NodeField) : WorkItem;
