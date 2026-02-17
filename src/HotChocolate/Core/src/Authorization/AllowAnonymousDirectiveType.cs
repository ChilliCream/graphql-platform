using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Authorization;

internal sealed class AllowAnonymousDirectiveType
    : DirectiveType
    , ISchemaDirective
{
    public AllowAnonymousDirectiveType()
    {
        Name = Names.AllowAnonymous;
    }

    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(Names.AllowAnonymous)
            .Location(DirectiveLocation.FieldDefinition)
            .Repeatable()
            .Internal();
    }

    public void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        ITypeSystemConfiguration definition,
        Stack<ITypeSystemConfiguration> path)
    {
        ((IDirectiveConfigurationProvider)definition).Directives.Add(new DirectiveConfiguration(directiveNode));

        if (definition is ObjectFieldConfiguration fieldDef)
        {
            fieldDef.ModifyAuthorizationFieldOptions(o => o with { AllowAnonymous = true });
        }
    }

    public static class Names
    {
        public const string AllowAnonymous = "allowAnonymous";
    }
}
