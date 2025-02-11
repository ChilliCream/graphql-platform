using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

internal sealed record MemberReference(InputFieldDefinition Argument, FieldNode Requirement);
