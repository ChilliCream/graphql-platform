using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
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

    protected sealed override SchemaTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        var descriptor = SchemaTypeDescriptor.New(context.DescriptorContext, GetType());

        _configure(descriptor);
        
        context.DescriptorContext.ApplySchemaConfigurations(descriptor);

        return descriptor.CreateDefinition();
    }

    protected override void OnAfterInitialize(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
    {
        base.OnAfterInitialize(context, definition);

        // we clear the configuration delegate to make sure that we do not hold on to any references
        // if we do not do this all the instances used during initialization will be kept in memory
        // until the schema is phased out.
        // We do this in OnAfterInitialized because after this point the schema is marked as
        // initialized. This means that a subsequent call to Initialize will throw anyway and
        // therefore we do not need to keep the configuration delegate.
        _configure = null;
    }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        SchemaTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        if (definition.HasDirectives)
        {
            foreach (var directive in definition.Directives)
            {
                context.Dependencies.Add(new(directive.Type, TypeDependencyFulfilled.Completed));
            }
        }

        foreach (var typeReference in definition.GetDirectives().Select(t => t.Type))
        {
            context.Dependencies.Add(new TypeDependency(typeReference));
        }
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        SchemaTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        Directives = DirectiveCollection.CreateAndComplete(
            context,
            this,
            definition.GetDirectives());
        Services = context.Services;
    }

    internal void CompleteSchema(SchemaTypesDefinition schemaTypesDefinition)
    {
        if (schemaTypesDefinition is null)
        {
            throw new ArgumentNullException(nameof(schemaTypesDefinition));
        }

        if (_sealed)
        {
            throw new InvalidOperationException(
                "This schema is already sealed and cannot be mutated.");
        }

        if (schemaTypesDefinition.Types is null || schemaTypesDefinition.DirectiveTypes is null)
        {
            throw new InvalidOperationException(
                "The schema type collections are not initialized.");
        }

        DirectiveTypes = schemaTypesDefinition.DirectiveTypes;
        _types = new SchemaTypes(schemaTypesDefinition);
        _directiveTypes = DirectiveTypes.ToDictionary(t => t.Name);
        _sealed = true;
    }
}

internal static class SchemaTools
{
    public static void AddSchemaConfiguration(
        this ISchemaBuilder builder,
        Action<ISchemaTypeDescriptor> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        List<Action<ISchemaTypeDescriptor>> options;

        if (!builder.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value))
        {
            options = [];
            builder.ContextData.Add(WellKnownContextData.InternalSchemaOptions, options);
            value = options;
        }

        options = (List<Action<ISchemaTypeDescriptor>>)value!;
        options.Add(configure);
    }
    
    public static void AddSchemaConfiguration(
        this IDescriptorContext context,
        Action<ISchemaTypeDescriptor> configure)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        List<Action<ISchemaTypeDescriptor>> options;

        if (!context.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value))
        {
            options = [];
            context.ContextData.Add(WellKnownContextData.InternalSchemaOptions, options);
            value = options;
        }

        options = (List<Action<ISchemaTypeDescriptor>>)value!;
        options.Add(configure);
    }
    
    public static void ApplySchemaConfigurations(
        this IDescriptorContext context,
        ISchemaTypeDescriptor descriptor)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (context.ContextData.TryGetValue(WellKnownContextData.InternalSchemaOptions, out var value) &&
            value is List<Action<ISchemaTypeDescriptor>> options)
        {
            foreach (var option in options)
            {
                option(descriptor);
            }
        }
    }
}