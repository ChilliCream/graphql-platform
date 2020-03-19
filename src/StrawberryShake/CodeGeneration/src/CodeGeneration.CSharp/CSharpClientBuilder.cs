using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;
using static StrawberryShake.CodeGeneration.Utilities.SerializerNameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpClientBuilder
    {
        private string? _namespace;
        private string? _name;
        private ClientModel? _model;

        public CSharpClientBuilder SetNamespace(string @namespace)
        {
            _namespace = @namespace;
            return this;
        }

        public CSharpClientBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public CSharpClientBuilder SetModel(ClientModel model)
        {
            _model = model;
            return this;
        }


    }

    internal static class DescriptorFactory
    {
        public static ClientClassDescriptor CreateClientClassDescriptor(
            ICSharpClientBuilderContext context,
            ClientModel model)
        {
            bool needsStreamExecutor = false;
            bool needsOperationExecutor = false;

            var operations = new List<ClientOperationMethodDescriptor>();

            foreach (OperationModel operation in model.Documents.SelectMany(t => t.Operations))
            {
                var arguments = new List<ClientOperationMethodParameterDescriptor>();

                foreach (ArgumentModel argument in operation.Arguments)
                {
                    bool isOptional =
                        argument.Type.IsNullableType()
                        || argument.DefaultValue is { };

                    arguments.Add(new ClientOperationMethodParameterDescriptor(
                        GetParameterName(argument.Name),
                        GetPropertyName(argument.Name),
                        context.GetFullTypeName(argument.Type, isOptional),
                        isOptional,
                        null));
                }

                bool isStream = operation.Operation.Operation == OperationType.Subscription;

                if (!isStream)
                {
                    needsOperationExecutor = true;
                }

                if (isStream)
                {
                    needsStreamExecutor = true;
                }

                operations.Add(new ClientOperationMethodDescriptor(
                    GetPropertyName(operation.Operation.Name!.Value),
                    operation.Name,
                    isStream,
                    context.GetFullTypeName(operation.Parser.ReturnType),
                    arguments));
            }

            return new ClientClassDescriptor(
                context.Name,
                context.Namespace,
                GetInterfaceName(context.Name),
                context.Types.IOperationExecutorPool,
                needsOperationExecutor ? context.Types.IOperationExecutor : null,
                needsStreamExecutor ? context.Types.IOperationStreamExecutor : null,
                operations);
        }

        public static EnumDescriptor CreateEnumDescriptor(
            ICSharpClientBuilderContext context,
            EnumTypeModel model)
        {
            return new EnumDescriptor(
                model.Name,
                context.Namespace,
                CreateEnumElementDescriptors(context, model));
        }

        public static EnumValueSerializerDescriptor CreateEnumValueSerializerDescriptor(
            ICSharpClientBuilderContext context,
            EnumTypeModel model)
        {
            return new EnumValueSerializerDescriptor(
                context.CreateFullTypeName($"{model.Name}ValueSerializer"),
                context.Namespace,
                model.Type.Name,
                context.GetFullTypeName((EnumType)model.Type),
                CreateEnumElementDescriptors(context, model));
        }

        private static IReadOnlyList<EnumElementDescriptor> CreateEnumElementDescriptors(
            ICSharpClientBuilderContext context,
            EnumTypeModel model)
        {
            var elements = new List<EnumElementDescriptor>();

            foreach (EnumValueModel value in model.Values)
            {
                elements.Add(new EnumElementDescriptor(
                    value.Name,
                    value.Value.Name,
                    value.UnderlyingValue is { } s ? (long?)long.Parse(s) : null));
            }

            return elements;
        }

        public static InputModelDescriptor CreateInputModelDescriptor(
            ICSharpClientBuilderContext context,
            ComplexInputTypeModel model)
        {
            var fields = new List<InputFieldDescriptor>();

            foreach (InputFieldModel field in model.Fields)
            {
                fields.Add(new InputFieldDescriptor(
                    field.Name,
                    context.GetFullTypeName(field.Type)));
            }

            return new InputModelDescriptor(
                model.Name,
                context.Namespace,
                fields);
        }

        public static InputModelSerializerDescriptor CreateInputModelSerializerDescriptor(
            ICSharpClientBuilderContext context,
            ComplexInputTypeModel model)
        {
            var typeNames = new HashSet<string>();
            var methodNames = new HashSet<string>();

            var fieldSerializers = new List<InputFieldSerializerDescriptor>();
            var typeSerializers = new List<InputTypeSerializerMethodDescriptor>();
            var valueSerializers = new List<ValueSerializerDescriptor>();

            foreach (InputFieldModel field in model.Fields)
            {
                string typeName = field.Type.NamedType().Print();
                string serializerName = CreateSerializerName(field.Type);

                fieldSerializers.Add(new InputFieldSerializerDescriptor(
                    field.Name,
                    field.Field.Name,
                    serializerName));

                if (typeNames.Add(typeName))
                {
                    valueSerializers.Add(new ValueSerializerDescriptor(
                        typeName,
                        CreateValueSerializerName(field.Type)));
                }

                RegisterTypeSerializer(serializerName, field.Type);
            }

            return new InputModelSerializerDescriptor(
                GetClassName(model.Name, "InputSerializer"),
                context.Namespace,
                model.Type.Name,
                model.Name,
                fieldSerializers,
                valueSerializers,
                typeSerializers);

            void RegisterTypeSerializer(string serializerName, IType type)
            {
                if (methodNames.Add(serializerName))
                {
                    string? innerSerializerName = type.IsListType()
                        ? CreateSerializerName(type.ListType().ElementType)
                        : null;

                    typeSerializers.Add(new InputTypeSerializerMethodDescriptor(
                        serializerName,
                        type.IsNullableType(),
                        type.IsListType(),
                        type.IsListType()
                            ? null
                            : CreateValueSerializerName(type.NamedType()),
                        innerSerializerName));

                    if (innerSerializerName is { })
                    {
                        RegisterTypeSerializer(innerSerializerName, type.ListType().ElementType);
                    }
                }
            }

            string CreateValueSerializerName(IType type) =>
                GetFieldName(type.NamedType().Print(), "Serializer");
        }

        public static OperationModelDescriptor CreateOperationModelDescriptor(
            ICSharpClientBuilderContext context,
            DocumentModel document,
            OperationModel model)
        {
            var arguments = new List<OperationArgumentDescriptor>();

            foreach (ArgumentModel argument in model.Arguments)
            {
                bool isOptional =
                    argument.Type.IsNullableType()
                        || argument.DefaultValue is { };

                arguments.Add(new OperationArgumentDescriptor(
                    GetPropertyName(argument.Name),
                    GetParameterName(argument.Name),
                    argument.Variable.Variable.Name.Value,
                    context.GetFullTypeName(argument.Type, isOptional),
                    argument.Type.NamedType().Print(),
                    isOptional));
            }

            return new OperationModelDescriptor(
                context.CreateTypeName(GetClassName(model.Name, "Operation")),
                context.Namespace,
                model.Operation.Name!.Value,
                context.GetFullTypeName(model.Parser.ReturnType),
                document.Name,
                model.Operation.Operation.ToString(),
                arguments);
        }
    }
}
