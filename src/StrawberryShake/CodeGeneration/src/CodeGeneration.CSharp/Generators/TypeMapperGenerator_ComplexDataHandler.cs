using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
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

            method
                .AddParameter(_dataParameterName)
                .SetType(complexTypeDescriptor.ParentRuntimeType.ToString())
                .SetName(_dataParameterName);

            if (!isNonNullable)
            {
                method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
            }

            const string returnValue = nameof(returnValue);
            method.AddCode($"{complexTypeDescriptor.RuntimeType.Name} {returnValue} = default!;");
            method.AddEmptyLine();

            GenerateIfForEachImplementedBy(
                method,
                complexTypeDescriptor,
                o => GenerateComplexDataInterfaceIfClause(o, returnValue));

            method.AddCode($"return {returnValue};");

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

            IfBuilder ifChain = generator(interfaceTypeDescriptor.ImplementedBy.First());

            foreach (ObjectTypeDescriptor objectTypeDescriptor in
                interfaceTypeDescriptor.ImplementedBy.Skip(1))
            {
                ifChain.AddIfElse(generator(objectTypeDescriptor).SkipIndents());
            }

            ifChain.AddElse(ExceptionBuilder.New(TypeNames.NotSupportedException));

            method.AddCode(ifChain);
        }

        private IfBuilder GenerateComplexDataInterfaceIfClause(
            ObjectTypeDescriptor objectTypeDescriptor,
            string variableName)
        {
            var matchedTypeName = GetParameterName(objectTypeDescriptor.Name);

            // since we want to create the data name we will need to craft the type name
            // by hand by using the GraphQL type name and the state namespace.
            var dataTypeName = new RuntimeTypeInfo(
                CreateDataTypeName(objectTypeDescriptor.Name),
                $"{objectTypeDescriptor.RuntimeType.Namespace}.State");

            MethodCallBuilder constructorCall = MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(objectTypeDescriptor.RuntimeType.ToString());

            foreach (PropertyDescriptor prop in objectTypeDescriptor.Properties)
            {
                if (prop.Type.IsEntityType())
                {
                    constructorCall.AddArgument(BuildMapMethodCall(matchedTypeName, prop));
                }
                else
                {
                    constructorCall.AddArgument($"{matchedTypeName}.{prop.Name}");
                }
            }

            return IfBuilder
                .New()
                .SetCondition($"{_dataParameterName} is {dataTypeName} {matchedTypeName}")
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide(variableName)
                        .SetRighthandSide(constructorCall));
        }
    }
}
