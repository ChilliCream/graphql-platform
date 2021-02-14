using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class TypeMapperGenerator
    {
        private const string DataParamName = "data";

        private void AddDataHandler(
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
                        $"global::{namedTypeDescriptor.Namespace}.State."
                        + NamingConventions.DataTypeNameFromTypeName(namedTypeDescriptor.GraphQLTypeName))
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

                if (isNonNullable)
                {
                    ifCorrectType.SetCondition(
                        $"{DataParamName}.__typename.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson})");
                }
                else
                {
                    ifCorrectType.SetCondition(
                        $"{DataParamName}?.__typename.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson}) ?? false");
                }


                var constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"{variableName} = new ")
                    .SetMethodName(interfaceImplementee.Name);

                foreach (PropertyDescriptor prop in interfaceImplementee.Properties)
                {
                    var propAccess = $"{DataParamName}.{prop.Name}";
                    if (prop.Type.IsEntityType())
                    {
                        constructorCall.AddArgument(
                            BuildMapMethodCall(
                                DataParamName,
                                prop,
                                true));
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
