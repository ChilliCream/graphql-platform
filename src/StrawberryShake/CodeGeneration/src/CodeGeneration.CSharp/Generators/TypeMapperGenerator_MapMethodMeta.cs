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
            CSharpSyntaxGeneratorSettings settings,
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
                        settings,
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
                        settings,
                        propAccess,
                        property.Type,
                        classBuilder,
                        constructorBuilder,
                        processed);

                    if (property.Type.NamedType() is ComplexTypeDescriptor ct &&
                        !ct.IsLeaf() && !stopAtEntityMappers)
                    {
                        AddRequiredMapMethods(
                            settings,
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
                    Kind: TypeKind.Entity,
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
            CSharpSyntaxGeneratorSettings settings,
            string propAccess,
            ITypeDescriptor typeReference,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            HashSet<string> processed)
        {
            string methodName = MapMethodNameFromTypeName(typeReference);

            if (!typeReference.IsLeaf() && processed.Add(methodName))
            {
                MethodBuilder methodBuilder = MethodBuilder
                    .New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetName(methodName)
                    .SetReturnType(typeReference.ToTypeReference());

                classBuilder.AddMethod(methodBuilder);

                AddMapMethodBody(
                    settings,
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
                .SetCondition($"{propertyName} is null")
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
            CSharpSyntaxGeneratorSettings settings,
            string objectName,
            PropertyDescriptor property,
            bool addNullCheck = false)
        {
            switch (property.Type.Kind)
            {
                case TypeKind.Leaf:
                    return CodeInlineBuilder.From($"{objectName}.{property.Name}");

                case TypeKind.AbstractData:
                case TypeKind.EntityOrData:
                case TypeKind.Data:
                case TypeKind.Entity:
                    MethodCallBuilder mapperMethodCall =
                        MethodCallBuilder
                            .Inline()
                            .SetMethodName(MapMethodNameFromTypeName(property.Type));

                    ICode argString = CodeInlineBuilder.From($"{objectName}.{property.Name}");
                    if (addNullCheck && property.Type.IsNonNullable())
                    {
                        argString = NullCheckBuilder
                            .Inline()
                            .SetCondition(argString)
                            .SetCode(ExceptionBuilder.Inline(TypeNames.ArgumentNullException));
                    }

                    return mapperMethodCall
                        .AddArgument(argString)
                        .If(settings.IsStoreEnabled(), x => x.AddArgument(_snapshot));

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddMapMethodBody(
            CSharpSyntaxGeneratorSettings settings,
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
                        settings,
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        listTypeDescriptor,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.Leaf }:
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.EntityOrData } d:
                    AddEntityOrUnionDataHandler(
                        settings,
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.AbstractData } d:
                    AddComplexDataHandler(
                        settings,
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.Data } d:
                    AddDataHandler(
                        settings,
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        d,
                        processed,
                        isNonNullable);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.Entity } d:
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
                        settings,
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
