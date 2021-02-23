using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class TypeMapperGenerator
    {
        private void AddComplexDataHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor complexTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            if (complexTypeDescriptor.ParentRuntimeType is null)
            {
                throw new InvalidOperationException();
            }

            method.AddParameter(
                ParameterBuilder.New()
                    .SetType(complexTypeDescriptor.ParentRuntimeType.ToString())
                    .SetName(_dataParameterName));

            if (!isNonNullable)
            {
                method.AddCode(
                    EnsureProperNullability(
                        _dataParameterName,
                        isNonNullable));
            }

            var variableName = "returnValue";
            method.AddCode($"{complexTypeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            GenerateIfForEachImplementedBy(
                method,
                complexTypeDescriptor,
                o => GenerateComplexDataInterfaceIfClause(o, variableName));

            method.AddCode($"return {variableName};");

            AddRequiredMapMethods(
                _dataParameterName,
                complexTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }

        private void GenerateIfForEachImplementedBy(
            MethodBuilder method,
            ComplexTypeDescriptor complexTypeDescriptor,
            Func<ObjectTypeDescriptor, IfBuilder> generator)
        {
            if (!(complexTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor) ||
                interfaceTypeDescriptor.ImplementedBy.Any())
            {
                return;
            }

            var ifChain = generator(interfaceTypeDescriptor.ImplementedBy[0]);

            foreach (ObjectTypeDescriptor objectTypeDescriptor in
                interfaceTypeDescriptor.ImplementedBy.Skip(1))
            {
                ifChain.AddIfElse(
                    generator(objectTypeDescriptor)
                        .SkipIndents());
            }

            ifChain.AddElse(
                CodeInlineBuilder.New()
                    .SetText($"throw new {TypeNames.NotSupportedException}();"));

            method.AddCode(ifChain);
        }

        private IfBuilder GenerateComplexDataInterfaceIfClause(
            ObjectTypeDescriptor objectTypeDescriptor,
            string variableName)
        {
            var ifCorrectType = IfBuilder.New();
            var matchedTypeName = objectTypeDescriptor.Name.WithLowerFirstChar();

            ifCorrectType.SetCondition(
                $"{_dataParameterName} is {objectTypeDescriptor.RuntimeType.Namespace}.State." +
                $"{DataTypeNameFromTypeName(objectTypeDescriptor.RuntimeType.Name)} " +
                $"{matchedTypeName}");


            var constructorCall = MethodCallBuilder.New()
                .SetPrefix($"{variableName} = new ")
                .SetMethodName(objectTypeDescriptor.Name);

            foreach (PropertyDescriptor prop in objectTypeDescriptor.Properties)
            {
                var propAccess = $"{matchedTypeName}.{prop.Name}";
                if (prop.Type.IsEntityType())
                {
                    constructorCall.AddArgument(
                        BuildMapMethodCall(
                            matchedTypeName,
                            prop));
                }
                else
                {
                    constructorCall.AddArgument(propAccess);
                }
            }

            return ifCorrectType.AddCode(constructorCall);
        }
    }
}
