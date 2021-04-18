using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        private void AddEntityOrUnionDataHandler(
            CSharpSyntaxGeneratorSettings settings,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor complexTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            method
                .AddParameter(_dataParameterName)
                .SetType(TypeNames.EntityIdOrData.MakeNullable(!isNonNullable))
                .SetName(_dataParameterName);

            method
                .AddParameter(_snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot)
                .SetName(_snapshot);

            if (!isNonNullable)
            {
                method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
            }

            var dataHandlerMethodName =
                MapMethodNameFromTypeName(complexTypeDescriptor) + "Entity";

            MethodBuilder complexDataHandler = MethodBuilder
                .New()
                .SetReturnType(
                    complexTypeDescriptor.RuntimeType.ToString().MakeNullable(!isNonNullable))
                .SetName(dataHandlerMethodName);

            AddComplexDataHandler(settings,
                classBuilder,
                constructorBuilder,
                complexDataHandler,
                complexTypeDescriptor,
                processed,
                isNonNullable);

            classBuilder.AddMethod(complexDataHandler);

            var entityDataHandlerMethodName =
                MapMethodNameFromTypeName(complexTypeDescriptor) + "Data";

            MethodBuilder entityDataHandler = MethodBuilder
                .New()
                .SetReturnType(
                    complexTypeDescriptor.RuntimeType.ToString().MakeNullable(!isNonNullable))
                .SetName(entityDataHandlerMethodName);

            AddEntityHandler(
                classBuilder,
                constructorBuilder,
                entityDataHandler,
                complexTypeDescriptor,
                processed,
                isNonNullable);

            classBuilder.AddMethod(entityDataHandler);

            method.AddEmptyLine();

            var parameterName = isNonNullable ? _dataParameterName : $"{_dataParameterName}.Value";

            IfBuilder ifBuilder = IfBuilder
                .New()
                .SetCondition($"{parameterName}.EntityId is {{ }}")
                .AddCode(MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(entityDataHandlerMethodName)
                    .AddArgument($"{parameterName}.EntityId")
                    .AddArgument(_snapshot))
                .AddIfElse(IfBuilder
                    .New()
                    .SetCondition(
                        $"{parameterName}.Data is {complexTypeDescriptor.ParentRuntimeType!} d")
                    .AddCode(MethodCallBuilder
                        .New()
                        .SetReturn()
                        .SetMethodName(dataHandlerMethodName)
                        .AddArgument("d")
                        .AddArgument(_snapshot)))
                .AddElse(ExceptionBuilder.New(TypeNames.ArgumentOutOfRangeException));

            method.AddCode(ifBuilder);

            AddRequiredMapMethods(
                settings,
                _dataParameterName,
                complexTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }
    }
}
