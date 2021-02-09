using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class ResultFromEntityTypeMapperGenerator
    {
        private const string DataParamName = "data";

        private void AddDataHandler(
            ITypeDescriptor rootDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            NamedTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed)
        {
            method.AddParameter(ParameterBuilder.New()
                .SetType(NamingConventions.DataTypeNameFromTypeName(namedTypeDescriptor.Name))
                .SetName(DataParamName)
            );

            method.AddCode(EnsureProperNullability(DataParamName, rootDescriptor.IsNonNullableType()));

            var variableName = "returnValue";
            method.AddCode($"{namedTypeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            var ifChain = InterfaceImplementeeIf(namedTypeDescriptor.ImplementedBy[0]);

            foreach (NamedTypeDescriptor interfaceImplementee in
                namedTypeDescriptor.ImplementedBy.Skip(1))
            {
                var singleIf = InterfaceImplementeeIf(interfaceImplementee).SkipIndents();
                ifChain.AddIfElse(singleIf);
            }

            ifChain.AddElse(CodeInlineBuilder.New()
                .SetText($"throw new {TypeNames.NotSupportedException}();"));

            method.AddCode(ifChain);

            IfBuilder InterfaceImplementeeIf(NamedTypeDescriptor interfaceImplementee)
            {
                var ifCorrectType = IfBuilder.New()
                    .SetCondition(
                        $"{DataParamName}?.__typename.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson}) ?? false");

                var constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"{variableName} = new ")
                    .SetMethodName(interfaceImplementee.Name);

                foreach (PropertyDescriptor prop in interfaceImplementee.Properties)
                {
                    var propAccess = $"{DataParamName}.{prop.Name}";
                    if (prop.Type.IsEntityType())
                    {
                        // $"{_storeFieldName}.GetEntities<{prop.Type.Name}>({propAccess})"
                        constructorCall.AddArgument(BuildMapMethodCall(DataParamName, prop));
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

            AddRequiredMapMethods(DataParamName, namedTypeDescriptor, classBuilder, constructorBuilder, processed);
        }
    }
}
