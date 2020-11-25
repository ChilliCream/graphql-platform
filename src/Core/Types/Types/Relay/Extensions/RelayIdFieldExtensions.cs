using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

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
                (c, d) => AddSerializerToInputField(c, d, typeName.Value));

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
                    (c, d) => AddSerializerToInputField(c, d, typeName.Value));

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

            FieldMiddleware placeholder = n => c => Task.CompletedTask;
            descriptor.Use(placeholder);
            descriptor.Extend().OnBeforeCreate(RewriteObjectFieldType);
            descriptor.Extend().OnBeforeCompletion(
                (c, d) => AddSerializerToObjectField(c, d, placeholder, typeName.Value));

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
            ArgumentDefinition definition)
        {
            var typeReference = (IClrTypeReference)definition.Type;
            Type rewritten = ExtendedTypeRewriter.RewriteToSchemaType(
                ExtendedType.FromType(typeReference.Type),
                typeof(IdType));
            definition.Type = typeReference.WithType(rewritten);
        }

        private static void AddSerializerToInputField(
            ICompletionContext completionContext,
            ArgumentDefinition definition,
            string? typeName)
        {
            Type resultType = definition.Parameter?.ParameterType ??
                ((ClrTypeReference)definition.Type).Type;

            if (definition is InputFieldDefinition inputFieldDefinition &&
                inputFieldDefinition.Property is { })
            {
                resultType = inputFieldDefinition.Property.PropertyType;
            }

            IdFieldValueSerializer serializer = CreateIdFieldValueSerializer(
                completionContext,
                resultType,
                typeName);

            definition.Serializer = serializer;

            var descriptorContext = (DescriptorContext)completionContext.DescriptorContext;

            descriptorContext.SchemaResolved += (sender, args) =>
            {
                ISchema schema = descriptorContext.ResolveSchema();

                NameString schemaName = schema.Name.HasValue
                    ? schema.Name
                    : Schema.DefaultName;

                serializer.Initialize(schemaName);
            };
        }

        private static void RewriteObjectFieldType(
            ObjectFieldDefinition definition)
        {
            var typeReference = (IClrTypeReference)definition.Type;
            Type rewritten = ExtendedTypeRewriter.RewriteToSchemaType(
                ExtendedType.FromType(typeReference.Type),
                typeof(IdType));
            definition.Type = typeReference.WithType(rewritten);
        }

        private static void RewriteInterfaceFieldType(
            InterfaceFieldDefinition definition)
        {
            var typeReference = (IClrTypeReference)definition.Type;
            Type rewritten = ExtendedTypeRewriter.RewriteToSchemaType(
                ExtendedType.FromType(typeReference.Type),
                typeof(IdType));
            definition.Type = typeReference.WithType(rewritten);
        }

        private static void AddSerializerToObjectField(
            ICompletionContext completionContext,
            ObjectFieldDefinition definition,
            FieldMiddleware placeholder,
            string? typeName)
        {
            IdFieldValueSerializer serializer = CreateIdFieldValueSerializer(
                completionContext,
                definition.ResultType,
                typeName);

            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = next => async context =>
            {
                await next(context).ConfigureAwait(false);
                context.Result = serializer.Serialize(context.Result);
            };

            var descriptorContext = (DescriptorContext)completionContext.DescriptorContext;

            descriptorContext.SchemaResolved += (sender, args) =>
            {
                ISchema schema = descriptorContext.ResolveSchema();

                NameString schemaName = schema.Name.HasValue
                    ? schema.Name
                    : Schema.DefaultName;

                serializer.Initialize(schemaName);
            };
        }

        private static IdFieldValueSerializer CreateIdFieldValueSerializer(
            ICompletionContext completionContext,
            Type type,
            string? typeName)
        {
            IIdSerializer innerSerializer =
                (IIdSerializer)completionContext.Services.GetService(typeof(IIdSerializer)) ??
                new IdSerializer();

            IIdFieldValueSerializerFactory idFieldValueSerializerFactory =
                (IIdFieldValueSerializerFactory)completionContext.Services.GetService(typeof(IIdFieldValueSerializerFactory)) ??
                new IdFieldValueSerializerFactory();

            return idFieldValueSerializerFactory.Create(
                typeName is { } ? typeName : completionContext.Type.Name.Value,
                innerSerializer,
                typeName is { },
                DotNetTypeInfoFactory.IsListType(type),
                DotNetTypeInfoFactory.GetInnerListType(type));
        }
    }
}
