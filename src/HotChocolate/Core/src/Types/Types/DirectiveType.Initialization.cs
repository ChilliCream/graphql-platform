using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Internal.FieldInitHelper;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A GraphQL schema describes directives which are used to annotate various parts of a
/// GraphQL document as an indicator that they should be evaluated differently by a
/// validator, executor, or client tool such as a code generator.
///
/// http://spec.graphql.org/draft/#sec-Type-System.Directives
/// </summary>
public partial class DirectiveType
{
    protected override DirectiveTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = DirectiveTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IDirectiveTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        DirectiveTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        RuntimeType = definition.RuntimeType == GetType()
            ? typeof(object)
            : definition.RuntimeType;

        if (RuntimeType != typeof(object))
        {
            TypeIdentity = typeof(DirectiveType<>).MakeGenericType(RuntimeType);
        }

        IsRepeatable = definition.IsRepeatable;

        TypeDependencyHelper.CollectDependencies(definition, context.Dependencies);
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        _inputParser = context.DescriptorContext.InputParser;
        _inputFormatter = context.DescriptorContext.InputFormatter;

        SyntaxNode = definition.SyntaxNode;
        Locations =  definition.Locations;
        Arguments = OnCompleteFields(context, definition);
        IsPublic = definition.IsPublic;
        Middleware = OnCompleteMiddleware(context, definition);

        _createInstance = OnCompleteCreateInstance(context, definition);
        _getFieldValues = OnCompleteGetFieldValues(context, definition);

        if (definition.Locations == 0)
        {
            // TODO : move to error helper
            context.ReportError(SchemaErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveType_NoLocations,
                    Name))
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(context.Type)
                .AddSyntaxNode(definition.SyntaxNode)
                .Build());
        }

        IsExecutableDirective = (Locations & DirectiveLocation.Executable) != 0;
        IsTypeSystemDirective = (Locations & DirectiveLocation.TypeSystem) != 0;
    }

    protected virtual FieldCollection<DirectiveArgument> OnCompleteFields(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        return CompleteFields(context, this, definition.GetArguments(), CreateArgument);
        static DirectiveArgument CreateArgument(DirectiveArgumentDefinition argDef, int index)
            => new(argDef, index);
    }

    protected virtual Func<object?[], object> OnCompleteCreateInstance(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        Func<object?[], object>? createInstance = null;

        if (definition.CreateInstance is not null)
        {
            createInstance = definition.CreateInstance;
        }

        if (RuntimeType == typeof(object) || Arguments.Any(t => t.Property is null))
        {
            createInstance ??= CreateDictionaryInstance;
        }
        else
        {
            createInstance ??= CompileFactory(this);
        }

        return createInstance;
    }

    protected virtual Action<object, object?[]> OnCompleteGetFieldValues(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        Action<object, object?[]>? getFieldValues = null;

        if (definition.GetFieldData is not null)
        {
            getFieldValues = definition.GetFieldData;
        }

        if (RuntimeType == typeof(object) || Arguments.Any(t => t.Property is null))
        {
            getFieldValues ??= CreateDictionaryGetValues;
        }
        else
        {
            getFieldValues ??= CompileGetFieldValues(this);
        }

        return getFieldValues;
    }

    protected virtual DirectiveMiddleware? OnCompleteMiddleware(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
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
