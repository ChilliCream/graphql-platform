using System.Collections.Frozen;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate;

public partial class Schema
{
    private Action<ISchemaTypeDescriptor> _configure;
    private bool _sealed;

    protected internal Schema()
    {
        _configure = Configure;
    }

    public Schema(Action<ISchemaTypeDescriptor> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    protected virtual void Configure(ISchemaTypeDescriptor descriptor) { }

    protected sealed override SchemaTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var descriptor = SchemaTypeDescriptor.New(context.DescriptorContext, GetType());

        _configure(descriptor);

        context.DescriptorContext.ApplySchemaConfigurations(descriptor);

        return descriptor.CreateConfiguration();
    }

    protected override void OnAfterInitialize(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
    {
        base.OnAfterInitialize(context, configuration);

        // we clear the configuration delegate to make sure that we do not hold on to any references
        // if we do not do this all the instances used during initialization will be kept in memory
        // until the schema is phased out.
        // We do this in OnAfterInitialized because after this point the schema is marked as
        // initialized. This means that a subsequent call to Initialize will throw anyway, and
        // therefore we do not need to keep the configuration delegate.
        _configure = null;
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        SchemaTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        if (configuration.HasDirectives)
        {
            foreach (var directive in configuration.Directives)
            {
                context.Dependencies.Add(new(directive.Type, TypeDependencyFulfilled.Completed));
            }
        }

        foreach (var typeReference in configuration.GetDirectives().Select(t => t.Type))
        {
            context.Dependencies.Add(new TypeDependency(typeReference));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        SchemaTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        Services = context.Services;
        Features = configuration.Features.ToReadOnly();
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        SchemaTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        Directives = DirectiveCollection.CreateAndComplete(context, this, configuration.GetDirectives());
    }

    internal void CompleteSchema(SchemaTypesConfiguration schemaTypesConfiguration)
    {
        if (schemaTypesConfiguration is null)
        {
            throw new ArgumentNullException(nameof(schemaTypesConfiguration));
        }

        if (_sealed)
        {
            throw new InvalidOperationException(
                "This schema is already sealed and cannot be mutated.");
        }

        if (schemaTypesConfiguration.Types is null || schemaTypesConfiguration.DirectiveTypes is null)
        {
            throw new InvalidOperationException(
                "The schema type collections are not initialized.");
        }

        DirectiveTypes = schemaTypesConfiguration.DirectiveTypes;
        _types = new SchemaTypes(schemaTypesConfiguration);
        _directiveTypes = DirectiveTypes.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
        _sealed = true;
    }
}
