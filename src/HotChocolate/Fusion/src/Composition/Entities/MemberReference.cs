using HotChocolate.Language;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal sealed record MemberReference(InputFieldDefinition Argument, FieldNode Requirement);
