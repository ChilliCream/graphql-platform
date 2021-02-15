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
            NamedTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            method.AddParameter(
                ParameterBuilder.New()
                    .SetType(
                        $"global::{namedTypeDescriptor.Namespace}.State.I" + 
                        DataTypeNameFromTypeName(namedTypeDescriptor.ComplexDataTypeParent!))
                    .SetName(DataParamName));

            if (!isNonNullable)
            {
                method.AddCode(
                    EnsureProperNullability(
                        DataParamName,
                        isNonNullable));
            }

            var variableName = "returnValue";
            method.AddCode($"{namedTypeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            if (namedTypeDescriptor.ImplementedBy.Any())
            {
                var ifChain = InterfaceImplementeeIf(namedTypeDescriptor.ImplementedBy[0]);

                foreach (NamedTypeDescriptor interfaceImplementee in
                    namedTypeDescriptor.ImplementedBy.Skip(1))
                {
                    var singleIf = InterfaceImplementeeIf(interfaceImplementee).SkipIndents();
                    ifChain.AddIfElse(singleIf);
                }

                ifChain.AddElse(
                    CodeInlineBuilder.New()
                        .SetText($"throw new {TypeNames.NotSupportedException}();"));

                method.AddCode(ifChain);
            }

            IfBuilder InterfaceImplementeeIf(NamedTypeDescriptor interfaceImplementee)
            {
                var ifCorrectType = IfBuilder.New();
                var matchedTypeName = interfaceImplementee.GraphQLTypeName.WithLowerFirstChar();

                ifCorrectType.SetCondition(
                    $"{DataParamName} is {interfaceImplementee.Namespace}.State." +
                    $"{DataTypeNameFromTypeName(interfaceImplementee.GraphQLTypeName)} " +
                    $"{matchedTypeName}");


                var constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"{variableName} = new ")
                    .SetMethodName(interfaceImplementee.Name);

                foreach (PropertyDescriptor prop in interfaceImplementee.Properties)
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

                ifCorrectType.AddCode(constructorCall);
                return ifCorrectType;
            }

            method.AddCode($"return {variableName};");

            AddRequiredMapMethods(
                DataParamName,
                namedTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }
    }
}
