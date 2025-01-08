using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// The <c>@override</c> directive designates that ownership of a field is transferred from one
/// source schema to another. In the context of interface types, fields are abstract—objects that
/// implement the interface are responsible for providing the actual fields. Consequently, it is
/// invalid to attach <c>@override</c> directly to an interface field. Doing so leads to an
/// <c>OVERRIDE_ON_INTERFACE</c> error because there is no concrete field implementation on the
/// interface itself that can be overridden.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Override-on-Interface">
/// Specification
/// </seealso>
internal sealed class OverrideOnInterfaceRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (type is InterfaceTypeDefinition && ValidationHelper.HasOverrideDirective(field))
        {
            context.Log.Write(OverrideOnInterface(field, type, schema));
        }
    }
}
