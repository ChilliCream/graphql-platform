using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities;

public record EntityFeature(SelectionSetNode Pattern);

public record LeafTypeInfo(string RuntimeType, string SerializationType);
