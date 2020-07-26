using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public class IDAttribute : DescriptorAttribute
    {
        public IDAttribute(string? typeName = null)
        {
            TypeName = typeName;
        }

        public string? TypeName { get; }

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IInputFieldDescriptor ifd
                && element is PropertyInfo)
            {
                var internalContext = (DescriptorContext)context;
                ifd.Extend().OnBeforeCreate(RewriteInputFieldType);
                ifd.Extend().OnBeforeCompletion(
                    (c, d) => AddSerializerToInputField(internalContext, c, d, TypeName));
            }

            if (descriptor is IArgumentDescriptor ad
                && element is ParameterInfo)
            {
                var internalContext = (DescriptorContext)context;
                ad.Extend().OnBeforeCreate(RewriteInputFieldType);
                ad.Extend().OnBeforeCompletion(
                    (c, d) => AddSerializerToInputField(internalContext, c, d, TypeName));
            }

            if (descriptor is IObjectFieldDescriptor ofd
                && element is MemberInfo)
            {
                var internalContext = (DescriptorContext)context;
                FieldMiddleware placeholder = n => c => Task.CompletedTask;
                ofd.Use(placeholder);
                ofd.Extend().OnBeforeCreate(RewriteObjectFieldType);
                ofd.Extend().OnBeforeCompletion(
                    (c, d) => AddSerializerToObjectField(
                        internalContext, c, d, placeholder, TypeName));
            }

            if (descriptor is IInterfaceFieldDescriptor infd
                && element is MemberInfo)
            {
                infd.Extend().OnBeforeCreate(RewriteInterfaceFieldType);
            }
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
            DescriptorContext descriptorContext,
            ICompletionContext completionContext,
            ArgumentDefinition definition,
            string? typeName)
        {
            var innerSerializer =
                (IIdSerializer)descriptorContext.Services.GetService(typeof(IIdSerializer)) ??
                new IdSerializer();

            FieldValueSerializer serializer = CreateSerializer(
                descriptorContext,
                completionContext,
                ((ClrTypeReference)definition.Type).Type,
                typeName);

            definition.Serializer = serializer;

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
            DescriptorContext descriptorContext,
            ICompletionContext completionContext,
            ObjectFieldDefinition definition,
            FieldMiddleware placeholder,
            string? typeName)
        {
            FieldValueSerializer serializer = CreateSerializer(
                descriptorContext,
                completionContext,
                ((ClrTypeReference)definition.Type).Type,
                typeName);

            int index = definition.MiddlewareComponents.IndexOf(placeholder);
            definition.MiddlewareComponents[index] = next => async context =>
            {
                await next(context).ConfigureAwait(false);
                context.Result = serializer.Serialize(context.Result);
            };

            descriptorContext.SchemaResolved += (sender, args) =>
            {
                ISchema schema = descriptorContext.ResolveSchema();

                NameString schemaName = schema.Name.HasValue
                    ? schema.Name
                    : Schema.DefaultName;

                serializer.Initialize(schemaName);
            };
        }

        private static FieldValueSerializer CreateSerializer(
            DescriptorContext descriptorContext,
            ICompletionContext completionContext,
            Type type,
            string? typeName)
        {
            var innerSerializer =
                (IIdSerializer)descriptorContext.Services.GetService(typeof(IIdSerializer)) ??
                new IdSerializer();

            return new FieldValueSerializer(
                typeName is { } ? typeName : completionContext.Type.Name.Value,
                innerSerializer,
                typeName is { },
                DotNetTypeInfoFactory.IsListType(type));
        }
    }
}