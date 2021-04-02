using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        private const string _dataParameterName = "data";

        private void AddDataHandler(
            CodeGeneratorSettings settings,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            method
                .AddParameter(_dataParameterName)
                .SetType(namedTypeDescriptor.ParentRuntimeType!
                    .ToString()
                    .MakeNullable(!isNonNullable));

            if (settings.IsStoreEnabled())
            {
                method
                    .AddParameter(_snapshot)
                    .SetType(TypeNames.IEntityStoreSnapshot);
            }

            if (!isNonNullable)
            {
                method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
            }

            const string returnValue = nameof(returnValue);

            method.AddCode($"{namedTypeDescriptor.RuntimeType.Name} {returnValue} = default!;");
            method.AddEmptyLine();

            GenerateIfForEachImplementedBy(
                method,
                namedTypeDescriptor,
                o => GenerateDataInterfaceIfClause(settings, o, isNonNullable, returnValue));

            method.AddCode($"return {returnValue};");

            AddRequiredMapMethods(
                settings,
                _dataParameterName,
                namedTypeDescriptor,
                classBuilder,
                constructorBuilder,
                processed);
        }

        private IfBuilder GenerateDataInterfaceIfClause(
            CodeGeneratorSettings settings,
            ObjectTypeDescriptor objectTypeDescriptor,
            bool isNonNullable,
            string variableName)
        {
            ICode ifCondition = MethodCallBuilder
                .Inline()
                .SetMethodName(
                    _dataParameterName.MakeNullable(!isNonNullable),
                    WellKnownNames.TypeName,
                    nameof(string.Equals))
                .AddArgument(objectTypeDescriptor.Name.AsStringToken())
                .AddArgument(TypeNames.OrdinalStringComparison);

            if (!isNonNullable)
            {
                ifCondition = NullCheckBuilder
                    .New()
                    .SetCondition(ifCondition)
                    .SetSingleLine()
                    .SetDetermineStatement(false)
                    .SetCode("false");
            }

            MethodCallBuilder constructorCall = MethodCallBuilder
                .Inline()
                .SetNew()
                .SetMethodName(objectTypeDescriptor.RuntimeType.Name);

            foreach (PropertyDescriptor prop in objectTypeDescriptor.Properties)
            {
                var propAccess = $"{_dataParameterName}.{prop.Name}";
                if (prop.Type.IsEntityType() || prop.Type.IsDataType())
                {
                    constructorCall.AddArgument(
                        BuildMapMethodCall(settings, _dataParameterName, prop, true));
                }
                else if (prop.Type.IsNullableType())
                {
                    constructorCall.AddArgument(propAccess);
                }
                else
                {
                    constructorCall
                        .AddArgument(
                            NullCheckBuilder
                                .Inline()
                                .SetCondition(propAccess)
                                .SetCode(ExceptionBuilder.Inline(TypeNames.ArgumentNullException)));
                }
            }

            return IfBuilder
                .New()
                .SetCondition(ifCondition)
                .AddCode(AssignmentBuilder
                    .New()
                    .SetLefthandSide(variableName)
                    .SetRighthandSide(constructorCall));
        }
    }
}
