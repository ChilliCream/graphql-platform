using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports an <c>@context</c> directive on an object, interface or union type whose <c>name</c>
/// argument does not start with a letter or contains a character other than letters and digits.
/// Such a usage raises a <c>CONTEXT_NAME_INVALID</c> error.
/// </summary>
internal sealed class ContextNameInvalidRule : IEventHandler<TypeEvent>
{
    public void Handle(TypeEvent @event, CompositionContext context)
    {
        var (type, schema) = @event;

        var directives = type switch
        {
            MutableComplexTypeDefinition complexType => complexType.Directives,
            MutableUnionTypeDefinition unionType => unionType.Directives,
            _ => null
        };

        if (directives is null)
        {
            return;
        }

        foreach (var directive in directives.AsEnumerable())
        {
            if (directive.Name == DirectiveNames.Context
                && directive.Arguments.TryGetValue(ArgumentNames.Name, out var value)
                && value is StringValueNode { Value: var name }
                && !IsValidName(name))
            {
                context.Log.Write(ContextNameInvalid(directive, type, name, schema));
            }
        }
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || !char.IsAsciiLetter(name[0]))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!char.IsAsciiLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
