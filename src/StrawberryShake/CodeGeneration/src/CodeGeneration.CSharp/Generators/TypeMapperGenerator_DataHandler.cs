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
        private const string _dataParameterName = "data";

        private void AddDataHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            method.AddParameter(
                _dataParameterName,
                x => x.SetType(
                        $"{namedTypeDescriptor.RuntimeType.Namespace}.State." +
                        DataTypeNameFromTypeName(namedTypeDescriptor.RuntimeType.Name)));

            if (!isNonNullable)
            {
                method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
            }

            var variableName = "returnValue";
            method.AddCode($"{namedTypeDescriptor.RuntimeType.Name} {variableName} = default!;");
            method.AddEmptyLine();

            GenerateIfForEachImplementedBy(
                method,
                namedTypeDescriptor,
                o => GenerateDataInterfaceIfClause(o, isNonNullable, variableName));

            method.AddCode($"return {variableName};");

            AddRequiredMapMethods(
                _dataParameterName,
                namedTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }

        private IfBuilder GenerateDataInterfaceIfClause(
            ObjectTypeDescriptor objectTypeDescriptor,
            bool isNonNullable,
            string variableName)
        {
            var ifCorrectType = IfBuilder.New();

            if (isNonNullable)
            {
                ifCorrectType.SetCondition(
                    $"{_dataParameterName}.__typename.Equals(\"" +
                    $"{objectTypeDescriptor.Name}\", " +
                    $"{TypeNames.OrdinalStringComparison})");
            }
            else
            {
                ifCorrectType.SetCondition(
                    $"{_dataParameterName}?.__typename.Equals(\"" +
                    $"{objectTypeDescriptor.Name}\", " +
                    $"{TypeNames.OrdinalStringComparison}) ?? false");
            }


            var constructorCall = MethodCallBuilder.New()
                .SetPrefix($"{variableName} = new ")
                .SetMethodName(objectTypeDescriptor.RuntimeType.Name);

            foreach (PropertyDescriptor prop in objectTypeDescriptor.Properties)
            {
                var propAccess = $"{_dataParameterName}.{prop.Name}";
                if (prop.Type.IsEntityType())
                {
                    constructorCall.AddArgument(BuildMapMethodCall(_dataParameterName, prop, true));
                }
                else
                {
                    constructorCall.AddArgument(
                        $"{propAccess} ?? throw new {TypeNames.ArgumentNullException}()");
                }
            }

            ifCorrectType.AddCode(constructorCall);
            return ifCorrectType;
        }
    }
}
