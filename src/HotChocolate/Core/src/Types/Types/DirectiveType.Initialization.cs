using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using HotChocolate.Utilities;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

#nullable enable

namespace HotChocolate.Types;

public partial class DirectiveType
{
    protected override DirectiveTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        try
        {
            if (Configuration is null)
            {
                var descriptor = DirectiveTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateConfiguration();
            }

            return Configuration;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IDirectiveTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        DirectiveTypeConfiguration configuration)
    {
        base.OnRegisterDependencies(context, configuration);

        RuntimeType = configuration.RuntimeType == GetType()
            ? typeof(object)
            : configuration.RuntimeType;

        if (RuntimeType != typeof(object))
        {
            TypeIdentity = typeof(DirectiveType<>).MakeGenericType(RuntimeType);
        }

        IsRepeatable = configuration.IsRepeatable;

        TypeDependencyHelper.CollectDependencies(configuration, context.Dependencies);
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration configuration)
    {
        base.OnCompleteType(context, configuration);

        _inputParser = context.DescriptorContext.InputParser;

        Locations = configuration.Locations;
        Arguments = OnCompleteFields(context, configuration);
        IsPublic = configuration.IsPublic;
        Middleware = OnCompleteMiddleware(context, configuration);

        _createInstance = OnCompleteCreateInstance(context, configuration);
        _getFieldValues = OnCompleteGetFieldValues(context, configuration);
        _parse = OnCompleteParse(context, configuration);
        _format = OnCompleteFormat(context, configuration);

        if (configuration.Locations == 0)
        {
            context.ReportError(ErrorHelper.DirectiveType_NoLocations(Name, this));
        }

        IsExecutableDirective = (Locations & DirectiveLocation.Executable) != 0;
        IsTypeSystemDirective = (Locations & DirectiveLocation.TypeSystem) != 0;
    }

    protected override void OnCompleteMetadata(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration configuration)
    {
        base.OnCompleteMetadata(context, configuration);

        foreach (IFieldCompletion field in Arguments)
        {
            field.CompleteMetadata(context, this);
        }
    }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration configuration)
    {
        base.OnMakeExecutable(context, configuration);

        foreach (IFieldCompletion field in Arguments)
        {
            field.MakeExecutable(context, this);
        }
    }

    protected override void OnFinalizeType(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration configuration)
    {
        base.OnFinalizeType(context, configuration);

        foreach (IFieldCompletion field in Arguments)
        {
            field.Finalize(context, this);
        }
    }

    protected virtual DirectiveArgumentCollection OnCompleteFields(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        return new DirectiveArgumentCollection(
            CompleteFields(
                context,
                this,
                definition.GetArguments(),
                CreateArgument));
        static DirectiveArgument CreateArgument(DirectiveArgumentConfiguration argDef, int index)
            => new(argDef, index);
    }

    protected virtual Func<object?[], object> OnCompleteCreateInstance(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        if (definition.CreateInstance is not null)
        {
            return definition.CreateInstance;
        }

        if (RuntimeType == typeof(object) || Arguments.Any(t => t.Property is null))
        {
            return CreateDictionaryInstance;
        }

        return CompileFactory(this);
    }

    protected virtual Action<object, object?[]> OnCompleteGetFieldValues(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        if (definition.GetFieldData is not null)
        {
            return definition.GetFieldData;
        }

        if (RuntimeType == typeof(object) || Arguments.Any(t => t.Property is null))
        {
            return CreateDictionaryGetValues;
        }

        return CompileGetFieldValues(this);
    }

    protected virtual Func<DirectiveNode, object> OnCompleteParse(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        if (definition.Parse is not null)
        {
            return definition.Parse;
        }

        var inputParser = context.DescriptorContext.InputParser;
        return node => inputParser.ParseDirective(node, this);
    }

    protected virtual Func<object, DirectiveNode> OnCompleteFormat(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        if (definition.Format is not null)
        {
            return definition.Format;
        }

        var inputFormatter = context.DescriptorContext.InputFormatter;
        return directive => inputFormatter.FormatDirective(directive, this);
    }

    protected virtual DirectiveMiddleware? OnCompleteMiddleware(
        ITypeCompletionContext context,
        DirectiveTypeConfiguration definition)
    {
        if (definition.MiddlewareComponents.Count == 0)
        {
            return null;
        }

        if (definition.MiddlewareComponents.Count == 1)
        {
            return definition.MiddlewareComponents[0];
        }

        return (initial, directive) =>
        {
            var next = initial;

            for (var i = definition.MiddlewareComponents.Count - 1; i >= 0; i--)
            {
                next = definition.MiddlewareComponents[i](next, directive);
            }

            return next;
        };
    }

    private object CreateDictionaryInstance(object?[] fieldValues)
    {
        var dictionary = new Dictionary<string, object?>();

        foreach (var field in Arguments.AsSpan())
        {
            dictionary.Add(field.Name, fieldValues[field.Index]);
        }

        return dictionary;
    }

    private void CreateDictionaryGetValues(object obj, object?[] fieldValues)
    {
        var map = (Dictionary<string, object?>)obj;

        foreach (var field in Arguments.AsSpan())
        {
            if (map.TryGetValue(field.Name, out var val))
            {
                fieldValues[field.Index] = val;
            }
        }
    }
}
