namespace HotChocolate.Fusion.Satisfiability;

internal readonly record struct WorkItem(VisitKey Key, PathNode? Path);
