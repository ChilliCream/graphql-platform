#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate;

public partial class Schema
{
    private AggregateSchemaDocumentFormatter? _formatter;
    private FrozenDictionary<string, ImmutableArray<ObjectType>> _possibleTypes = null!;
    private Action<ISchemaTypeDescriptor>? _configure;
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

        _configure?.Invoke(descriptor);

        context.DescriptorContext.ApplySchemaConfigurations(descriptor);

        return descriptor.CreateConfiguration();
    }

    protected override void OnAfterInitialize(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
    {
        base.OnAfterInitialize(context, configuration);

        // We clear the configuration delegate to make sure that we do not hold on to any references.
        // This ensures that all the instances used during initialization will be garbage collected.
        // We do this in OnAfterInitialized because after this point the schema is marked as
        // initialized. This means that a later call to Initialize will throw anyway, and
        // therefore we do not need to keep the configuration delegate.
        _configure = null;
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        SchemaTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        foreach (var directive in configuration.GetDirectives())
        {
            context.Dependencies.Add(new TypeDependency(directive.Type, TypeDependencyFulfilled.Completed));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        SchemaTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        Services = context.Services;
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
        ArgumentNullException.ThrowIfNull(schemaTypesConfiguration);

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

        QueryType = schemaTypesConfiguration.QueryType!;
        MutationType = schemaTypesConfiguration.MutationType;
        SubscriptionType = schemaTypesConfiguration.SubscriptionType;
        Types = new TypeDefinitionCollection(schemaTypesConfiguration.Types ?? []);
        _possibleTypes = CreatePossibleTypeLookup(schemaTypesConfiguration.Types ?? []);
        DirectiveTypes = new DirectiveTypeCollection(schemaTypesConfiguration.DirectiveTypes ?? []);
        _sealed = true;
    }

    private static FrozenDictionary<string, ImmutableArray<ObjectType>> CreatePossibleTypeLookup(
        ITypeDefinition[] types)
    {
        var possibleTypes = new Dictionary<string, ImmutableArray<ObjectType>.Builder>(StringComparer.Ordinal);

        foreach (var type in types)
        {
            switch (type)
            {
                case ObjectType ot:
                    var builder = ImmutableArray.CreateBuilder<ObjectType>();
                    builder.Add(ot);
                    possibleTypes[ot.Name] = builder;

                    foreach (var interfaceType in ot.Implements)
                    {
                        if (!possibleTypes.TryGetValue(interfaceType.Name, out var pt))
                        {
                            pt = ImmutableArray.CreateBuilder<ObjectType>();
                            possibleTypes[interfaceType.Name] = pt;
                        }

                        pt.Add(ot);
                    }

                    break;

                case UnionType ut:
                    foreach (var objectType in ut.Types)
                    {
                        if (!possibleTypes.TryGetValue(ut.Name, out var pt))
                        {
                            pt = ImmutableArray.CreateBuilder<ObjectType>();
                            possibleTypes[ut.Name] = pt;
                        }

                        pt.Add(objectType);
                    }

                    break;
            }
        }

        return possibleTypes.ToFrozenDictionary(k => k.Key, v => v.Value.ToImmutable());
    }
}
