using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        protected void AddRequiredMapMethods(
            string propAccess,
            ComplexTypeDescriptor typeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            HashSet<string> processed,
            bool stopAtEntityMappers = false)
        {
            if (typeDescriptor is InterfaceTypeDescriptor interfaceType)
            {
                foreach (var objectTypeDescriptor in interfaceType.ImplementedBy)
                {
                    AddRequiredMapMethods(
                        propAccess,
                        objectTypeDescriptor,
                        classBuilder,
                        constructorBuilder,
                        processed);
                }
            }
            else
            {
                foreach (var property in typeDescriptor.Properties)
                {
                    AddMapMethod(
                        propAccess,
                        property.Type,
                        classBuilder,
                        constructorBuilder,
                        processed);

                    if (property.Type.NamedType() is ComplexTypeDescriptor ct &&
                        !ct.IsLeafType() && !stopAtEntityMappers)
                    {
                        AddRequiredMapMethods(
                            propAccess,
                            ct,
                            classBuilder,
                            constructorBuilder,
                            processed);
                    }
                }
            }
        }

        private static string MapMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        {
            return "Map" + BuildMapMethodName(typeDescriptor);
        }

        private static string BuildMapMethodName(
            ITypeDescriptor typeDescriptor,
            bool parentIsList = false)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    BuildMapMethodName(listTypeDescriptor.InnerType, true) + "Array",

                ILeafTypeDescriptor leafTypeDescriptor =>
                    GetPropertyName(leafTypeDescriptor.Name),

                InterfaceTypeDescriptor
                {
                    ImplementedBy: { Count: > 1 },
                    ParentRuntimeType: { } parentRuntimeType
                } => parentRuntimeType!.Name,

                INamedTypeDescriptor namedTypeDescriptor =>
                    namedTypeDescriptor.RuntimeType.Name,

                NonNullTypeDescriptor nonNullTypeDescriptor => parentIsList
                    ? BuildMapMethodName(nonNullTypeDescriptor.InnerType) + "NonNullable"
                    : "NonNullable" + BuildMapMethodName(nonNullTypeDescriptor.InnerType),

                _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor))
            };
        }

        private void AddMapMethod(
            string propAccess,
            ITypeDescriptor typeReference,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            HashSet<string> processed)
        {
            string methodName = MapMethodNameFromTypeName(typeReference);

            if (!typeReference.IsLeafType() && processed.Add(methodName))
            {
                MethodBuilder methodBuilder = MethodBuilder
                    .New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetName(methodName)
                    .SetReturnType(typeReference.ToBuilder());

                classBuilder.AddMethod(methodBuilder);

                AddMapMethodBody(
                    classBuilder,
                    constructorBuilder,
                    methodBuilder,
                    typeReference,
                    processed);
            }
        }

        private CodeBlockBuilder EnsureProperNullability(
            string propertyName,
            bool isNonNullType = false)
        {
            IfBuilder ifBuilder = IfBuilder
                .New()
                .SetCondition($"{propertyName} == default")
                .AddCode(
                    isNonNullType
                        ? ExceptionBuilder.New(TypeNames.ArgumentNullException)
                        : CodeLineBuilder.From("return null;"));

            return CodeBlockBuilder
                .New()
                .AddCode(ifBuilder)
                .AddEmptyLine();
        }

        protected ICode BuildMapMethodCall(
            string objectName,
            PropertyDescriptor property,
            bool addNullCheck = false)
        {
            switch (property.Type.Kind)
            {
                case TypeKind.LeafType:
                    return CodeInlineBuilder.From($"{objectName}.{property.Name}");

                case TypeKind.ComplexDataType:
                case TypeKind.DataType:
                case TypeKind.EntityType:
                    MethodCallBuilder mapperMethodCall =
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(MapMethodNameFromTypeName(property.Type));

                    ICode argString = CodeInlineBuilder.From($"{objectName}.{property.Name}");
                    if (addNullCheck && property.Type.IsNonNullableType())
                    {
                        argString = NullCheckBuilder
                            .Inline()
                            .SetCondition(argString)
                            .SetCode(ExceptionBuilder.Inline(TypeNames.ArgumentNullException));
                    }

                    mapperMethodCall.AddArgument(argString);

                    return mapperMethodCall;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddMapMethodBody(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder methodBuilder,
            ITypeDescriptor typeDescriptor,
            HashSet<string> processed,
            bool isNonNullable = false)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddArrayHandler(
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        listTypeDescriptor,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.LeafType }:
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.ComplexDataType } d:
                    AddComplexDataHandler(
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.DataType } d:
                    AddDataHandler(
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.EntityType } d:
                    AddEntityHandler(
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    AddMapMethodBody(
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        nonNullTypeDescriptor.InnerType,
                        processed,
                        true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }
    }
}
