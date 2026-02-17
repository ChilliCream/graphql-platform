using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities;

public record EntityFeature(SelectionSetNode Pattern);

public record LeafTypeFeature(string? RuntimeType, string SerializationType);
