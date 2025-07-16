using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed record VariableValues(Path Path, ObjectValueNode Values);
