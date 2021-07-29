using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types
{
    public static class RelayIdFieldExtensions
    {
        private static IdSerializer? _idSerializer;

        [Obsolete("Use GlobalId")]
        public static IInputFieldDescriptor ID(
            this IInputFieldDescriptor descriptor,
            NameString typeName = default)
            => descriptor.GlobalId(typeName);

        [Obsolete("Use GlobalId")]
        public static IArgumentDescriptor ID(
            this IArgumentDescriptor descriptor,
            NameString typeName = default)
            => descriptor.GlobalId(typeName);

        [Obsolete("Use GlobalId")]
        public static IObjectFieldDescriptor ID(
            this IObjectFieldDescriptor descriptor,
            NameString typeName = default)
            => descriptor.GlobalId(typeName);

        [Obsolete("Use GlobalId")]
        public static IInterfaceFieldDescriptor ID(
            this IInterfaceFieldDescriptor descriptor)
            => descriptor.GlobalId();

        public static IInputFieldDescriptor GlobalId(
            this IInputFieldDescriptor descriptor,
            NameString typeName = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().OnBeforeCreate(RewriteInputFieldType);
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => AddSerializerToInputField(c, d, typeName));

            return descriptor;
        }

        public static IArgumentDescriptor GlobalId(
            this IArgumentDescriptor descriptor,
            NameString typeName = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().OnBeforeCreate(RewriteInputFieldType);
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => AddSerializerToInputField(c, d, typeName));

            return descriptor;
        }

        public static IObjectFieldDescriptor GlobalId(
            this IObjectFieldDescriptor descriptor,
            NameString typeName = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ResultConverterDefinition placeholder =
                new((_, r) => r, key: WellKnownMiddleware.GlobalId, isRepeatable: false);

            descriptor.Extend().Definition.ResultConverters.Add(placeholder);
            descriptor.Extend().OnBeforeCreate(RewriteObjectFieldType);
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => AddSerializerToObjectField(c, d, placeholder, typeName));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor GlobalId(
            this IInterfaceFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.Extend().OnBeforeCreate(RewriteInterfaceFieldType);

            return descriptor;
        }

        private static void RewriteInputFieldType(
            IDescriptorContext context,
            ArgumentDefinition definition)
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

        private static void RewriteObjectFieldType(
            IDescriptorContext context,
            ObjectFieldDefinition definition)
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

        private static void RewriteInterfaceFieldType(
            IDescriptorContext context,
            InterfaceFieldDefinition definition)
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

            if (definition is InputFieldDefinition inputField)
            {
                resultType = typeInspector.GetReturnType(inputField.Property!, true);
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
            }, key: WellKnownMiddleware.GlobalId, isRepeatable: false);
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
}
