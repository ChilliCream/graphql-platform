using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public static class RelayIdFieldExtensions
    {
        public static IInputFieldDescriptor ID(
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

        public static IArgumentDescriptor ID(
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

        public static IObjectFieldDescriptor ID(
            this IObjectFieldDescriptor descriptor,
            NameString typeName = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            FieldMiddleware placeholder = n => c => default;
            descriptor.Use(placeholder);
            descriptor.Extend().OnBeforeCreate(RewriteObjectFieldType);
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => AddSerializerToObjectField(c, d, placeholder, typeName));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor ID(
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
            if(definition.Type is ExtendedTypeReference typeReference)
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
            if(definition.Type is ExtendedTypeReference typeReference)
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
            if(definition.Type is ExtendedTypeReference typeReference)
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
            FieldMiddleware placeholder,
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
            completionContext.DescriptorContext.SchemaCompleted += (sender, args) =>
                schemaName = args.Schema.Name;

            IIdSerializer serializer =
                completionContext.Services.GetService<IIdSerializer>() ??
                new IdSerializer();
            var index = definition.MiddlewareComponents.IndexOf(placeholder);

            if (typeName.IsEmpty)
            {
                typeName = completionContext.Type.Name;
            }

            definition.MiddlewareComponents[index] = next => async context =>
            {
                await next(context).ConfigureAwait(false);

                if (context.Result is not null)
                {
                    if (resultType.IsArrayOrList)
                    {
                        var list = new List<object?>();
                        foreach (object? element in (IEnumerable)context.Result)
                        {
                            list.Add(element is null
                                ? element
                                : serializer.Serialize(schemaName, typeName, element));
                        }
                        context.Result = list;
                    }
                    else
                    {
                        context.Result = serializer.Serialize(schemaName, typeName, context.Result);
                    }
                }
            };
        }

        private static IInputValueFormatter CreateSerializer(
            ITypeCompletionContext completionContext,
            IExtendedType resultType,
            NameString typeName)
        {
            IIdSerializer serializer =
                completionContext.Services.GetService<IIdSerializer>() ??
                new IdSerializer();

            return new GlobalIdInputValueFormatter(
                typeName.HasValue ? typeName : completionContext.Type.Name,
                serializer,
                resultType,
                typeName.HasValue);
        }
    }
}
