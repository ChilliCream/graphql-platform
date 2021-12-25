using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types.Relay;

internal static class RelayIdFieldHelpers
{
    private static IdSerializer? _idSerializer;

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to a argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    /// <param name="descriptor"></param>
    /// <param name="typeName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ApplyIdToField(
        IDescriptor<ArgumentDefinition> descriptor,
        NameString typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }


        descriptor.Extend().OnBeforeCreate(RewriteDefinition);
        descriptor.Extend()
            .OnBeforeCompletion(
                (c, d) => AddSerializerToInputField(c, d, typeName));
    }

    /// <summary>
    /// Applies the <see cref="RelayIdFieldExtensions"><c>.ID()</c></see> to a argument
    /// descriptor
    /// </summary>
    /// <remarks>
    /// You most likely want to call `<c>.ID()</c>` directly and do not use this helper
    /// </remarks>
    /// <param name="descriptor"></param>
    /// <param name="typeName"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ApplyIdToField(
        IDescriptor<OutputFieldDefinitionBase> descriptor,
        NameString typeName = default)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.Extend().OnBeforeCreate(RewriteDefinition);
        if (descriptor is IDescriptor<ObjectFieldDefinition> objectFieldDescriptor)
        {
            ResultConverterDefinition placeholder =
                new((_, r) => r, key: WellKnownMiddleware.GlobalId, isRepeatable: false);
            objectFieldDescriptor.Extend().Definition.ResultConverters.Add(placeholder);
            objectFieldDescriptor.Extend()
                .OnBeforeCompletion(
                    (c, d) => AddSerializerToObjectField(c, d, placeholder, typeName));
        }
    }

    private static void RewriteDefinition(
        IDescriptorContext context,
        FieldDefinitionBase definition)
    {
        if (definition.Type is ExtendedTypeReference typeReference)
        {
            ITypeInfo typeInfo = context.TypeInspector.CreateTypeInfo(typeReference.Type);
            IExtendedType type = RewriteType(context.TypeInspector, typeInfo);
            definition.Type = typeReference.WithType(type);
        }
        else
        {
            throw new SchemaException(SchemaErrorBuilder.New()
                .SetMessage("Unable to resolve type from field `{0}`.", definition.Name)
                .Build());
        }
    }

    private static IExtendedType RewriteType(ITypeInspector typeInspector, ITypeInfo typeInfo)
    {
        Type current = typeof(IdType);

        if (typeInfo.Components.Count > 1)
        {
            foreach (TypeComponent component in typeInfo.Components.Reverse().Skip(1))
            {
                if (component.Kind == TypeComponentKind.NonNull)
                {
                    current = typeof(NonNullType<>).MakeGenericType(current);
                }
                else if (component.Kind == TypeComponentKind.List)
                {
                    current = typeof(ListType<>).MakeGenericType(current);
                }
            }
        }

        return typeInspector.GetType(current);
    }

    private static void AddSerializerToInputField(
        ITypeCompletionContext completionContext,
        ArgumentDefinition definition,
        NameString typeName)
    {
        ITypeInspector typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (definition is InputFieldDefinition { RuntimeType: { } runtimeType })
        {
            resultType = typeInspector.GetType(runtimeType);
        }
        else if (definition is InputFieldDefinition { Property: not null } inputField)
        {
            resultType = typeInspector.GetReturnType(inputField.Property, true);
        }
        else if (definition.Parameter is not null)
        {
            resultType = typeInspector.GetArgumentType(definition.Parameter, true);
        }
        else if (definition.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw new SchemaException(SchemaErrorBuilder.New()
                .SetMessage("Unable to resolve type from field `{0}`.", definition.Name)
                .SetTypeSystemObject(completionContext.Type)
                .Build());
        }

        definition.Formatters.Add(CreateSerializer(completionContext, resultType, typeName));
    }

    private static void AddSerializerToObjectField(
        ITypeCompletionContext completionContext,
        ObjectFieldDefinition definition,
        ResultConverterDefinition placeholder,
        NameString typeName)
    {
        ITypeInspector typeInspector = completionContext.TypeInspector;
        IExtendedType? resultType;

        if (definition.ResultType is not null)
        {
            resultType = typeInspector.GetType(definition.ResultType);
        }
        else if (definition.Type is ExtendedTypeReference typeReference)
        {
            resultType = typeReference.Type;
        }
        else
        {
            throw new SchemaException(SchemaErrorBuilder.New()
                .SetMessage("Unable to resolve type from field `{0}`.", definition.Name)
                .SetTypeSystemObject(completionContext.Type)
                .Build());
        }

        NameString schemaName = default;
        completionContext.DescriptorContext.SchemaCompleted += (_, args) =>
            schemaName = args.Schema.Name;

        IIdSerializer serializer =
            completionContext.Services.GetService<IIdSerializer>() ??
            new IdSerializer();
        var index = definition.ResultConverters.IndexOf(placeholder);

        if (typeName.IsEmpty)
        {
            typeName = completionContext.Type.Name;
        }

        definition.ResultConverters[index] = new((_, result) =>
            {
                if (result is not null)
                {
                    if (resultType.IsArrayOrList)
                    {
                        var list = new List<object?>();

                        foreach (var element in (IEnumerable)result)
                        {
                            list.Add(element is null
                                ? element
                                : serializer.Serialize(schemaName, typeName, element));
                        }

                        return list;
                    }

                    return serializer.Serialize(schemaName, typeName, result);
                }

                return result;
            },
            key: WellKnownMiddleware.GlobalId,
            isRepeatable: false);
    }

    private static IInputValueFormatter CreateSerializer(
        ITypeCompletionContext completionContext,
        IExtendedType resultType,
        NameString typeName)
    {
        IIdSerializer serializer =
            completionContext.Services.GetService<IIdSerializer>() ??
            (_idSerializer ??= new IdSerializer());

        return new GlobalIdInputValueFormatter(
            typeName.HasValue ? typeName : completionContext.Type.Name,
            serializer,
            resultType,
            typeName.HasValue);
    }
}
