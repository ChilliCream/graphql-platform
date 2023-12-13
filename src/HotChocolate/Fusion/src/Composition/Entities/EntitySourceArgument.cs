using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition;

internal readonly record struct EntitySourceArgument(InputField Argument, IsDirective Directive);