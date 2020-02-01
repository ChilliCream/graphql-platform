using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.Generators.Descriptors;
using static StrawberryShake.Generators.Utilities.NameUtils;

namespace StrawberryShake.Generators
{
    internal class OperationModelGenerator
    {
        public ICodeDescriptor Generate(
            IModelGeneratorContext context,
            ObjectType operationType,
            OperationDefinitionNode operation,
            ICodeDescriptor resultType)
        {
            var arguments = new List<Descriptors.IArgumentDescriptor>();

            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
            {
                string typeName = variableDefinition.Type.NamedType().Name.Value;

                if (!context.Schema.TryGetType(typeName, out INamedType namedType))
                {
                    throw new InvalidOperationException(
                        $"The variable type `{typeName}` is not supported by the schema.");
                }

                IType type = variableDefinition.Type.ToType(namedType);
                IInputClassDescriptor? inputClassDescriptor = null;

                if (namedType is InputObjectType inputObjectType)
                {
                    inputClassDescriptor = GenerateInputObjectType(context, inputObjectType);
                }

                arguments.Add(new ArgumentDescriptor(
                    variableDefinition.Variable.Name.Value,
                    type,
                    variableDefinition,
                    inputClassDescriptor));
            }

            string operationName = context.GetOrCreateName(
                operation,
                GetClassName(operation.Name!.Value) + "Operation");

            var descriptor = new OperationDescriptor(
                operationName,
                context.Namespace,
                operationType,
                operation,
                arguments,
                context.Query,
                resultType);

            context.Register(descriptor);

            return descriptor;
        }

        private IInputClassDescriptor GenerateInputObjectType(
            IModelGeneratorContext context,
            InputObjectType inputObjectType)
        {
            return GenerateInputObjectType(
                context,
                inputObjectType,
                new Dictionary<string, IInputClassDescriptor>());
        }

        private IInputClassDescriptor GenerateInputObjectType(
            IModelGeneratorContext context,
            InputObjectType inputObjectType,
            IDictionary<string, IInputClassDescriptor> knownTypes)
        {
            if (knownTypes.TryGetValue(
                inputObjectType.Name,
                out IInputClassDescriptor? descriptor))
            {
                return descriptor;
            }

            string typeName = context.GetOrCreateName(
                inputObjectType.SyntaxNode,
                GetClassName(inputObjectType.Name));

            var fields = new List<Descriptors.IInputFieldDescriptor>();
            descriptor = new InputClassDescriptor(
                typeName, context.Namespace, inputObjectType, fields);
            knownTypes[inputObjectType.Name] = descriptor;

            foreach (InputField field in inputObjectType.Fields)
            {
                if (field.Type.NamedType() is InputObjectType fieldType)
                {
                    fields.Add(new InputFieldDescriptor(
                        field.Name, field.Type, field,
                        GenerateInputObjectType(context, fieldType, knownTypes)));
                }
                else
                {
                    fields.Add(new InputFieldDescriptor(
                        field.Name, field.Type, field, null));
                }
            }

            return descriptor;
        }
    }
}
