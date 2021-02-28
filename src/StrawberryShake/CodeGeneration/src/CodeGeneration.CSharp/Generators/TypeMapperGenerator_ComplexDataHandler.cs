using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

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
            method.AddCode($"{complexTypeDescriptor.RuntimeType.Name} {variableName} = default!;");
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
                !interfaceTypeDescriptor.ImplementedBy.Any())
            {
                return;
            }

            var ifChain = generator(interfaceTypeDescriptor.ImplementedBy.First());

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
            var matchedTypeName = GetParameterName(objectTypeDescriptor.Name);
            
            // since we want to create the data name we will need to craft the type name
            // by hand by using the GraphQL type name and the state namespace.
            // TODO : state namespace should be available here!
            var dataTypeName = new RuntimeTypeInfo(
                CreateDataTypeName(objectTypeDescriptor.Name),
                $"{objectTypeDescriptor.RuntimeType.Namespace}.State");

            ifCorrectType.SetCondition(
                $"{_dataParameterName} is {dataTypeName} {matchedTypeName}");

            var constructorCall = MethodCallBuilder.New()
                .SetPrefix($"{variableName} = new ")
                .SetMethodName(objectTypeDescriptor.RuntimeType.ToString());

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
