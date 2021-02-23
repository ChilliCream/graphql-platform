using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
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
            var ret = "Map";
            ret += BuildMapMethodName(typeDescriptor);
            return ret;
        }

        private static string BuildMapMethodName(
            ITypeDescriptor typeDescriptor,
            bool parentIsList = false)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    return BuildMapMethodName(
                               listTypeDescriptor.InnerType,
                               true) +
                           "Array";

                case NamedTypeDescriptor namedTypeDescriptor:
                    return namedTypeDescriptor.Kind switch
                    {
                        TypeKind.LeafType => typeDescriptor.Name.WithCapitalFirstChar(),
                        TypeKind.DataType => typeDescriptor.Name,
                        TypeKind.ComplexDataType => namedTypeDescriptor.ImplementedBy.Count > 1
                            ? namedTypeDescriptor.ComplexDataTypeParent!
                            : typeDescriptor.Name,
                        TypeKind.EntityType => typeDescriptor.Name,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    return parentIsList
                        ? BuildMapMethodName(nonNullTypeDescriptor.InnerType) +
                          "NonNullable"
                        : "NonNullable" +
                          BuildMapMethodName(nonNullTypeDescriptor.InnerType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
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
                var returnType = typeReference.ToBuilder();

                var methodBuilder = MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetName(methodName)
                    .SetReturnType(returnType);

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
            var ifBuilder = IfBuilder
                .New()
                .SetCondition(
                    ConditionBuilder.New()
                        .Set($"{propertyName} == default"));
            ifBuilder.AddCode(
                isNonNullType
                    ? $"throw new {TypeNames.ArgumentNullException}();"
                    : "return null;");

            var codeBuilder = CodeBlockBuilder.New()
                .AddCode(ifBuilder)
                .AddEmptyLine();

            return codeBuilder;
        }

        protected ICode BuildMapMethodCall(
            string objectName,
            PropertyDescriptor property,
            bool wasCalledFromDataHandler = false)
        {
            switch (property.Type.Kind)
            {
                case TypeKind.LeafType:
                    return CodeInlineBuilder.New().SetText($"{objectName}.{property.Name}");
                case TypeKind.ComplexDataType:
                case TypeKind.DataType:
                case TypeKind.EntityType:
                    var mapperMethodCall =
                        MethodCallBuilder
                            .New()
                            .SetDetermineStatement(false)
                            .SetMethodName(MapMethodNameFromTypeName(property.Type));

                    var argString = $"{objectName}.{property.Name}";
                    if (wasCalledFromDataHandler && property.Type.IsNonNullableType())
                    {
                        argString += $" ?? throw new {TypeNames.ArgumentNullException}()";
                    }
                    mapperMethodCall.AddArgument(argString);

                    return mapperMethodCall;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected MethodCallBuilder BuildMapMethodCall(ITypeDescriptor property, string firstArg)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(MapMethodNameFromTypeName(property));

            deserializeMethodCaller.AddArgument(firstArg);

            return deserializeMethodCaller;
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

                case ComplexTypeDescriptor complexTypeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.LeafType:
                            break;

                        case TypeKind.ComplexDataType:
                            AddComplexDataHandler(
                                classBuilder,
                                constructorBuilder,
                                methodBuilder,
                                complexTypeDescriptor,
                                processed,
                                isNonNullable);
                            break;

                        case TypeKind.DataType:
                            AddDataHandler(
                                classBuilder,
                                constructorBuilder,
                                methodBuilder,
                                complexTypeDescriptor,
                                processed,
                                isNonNullable);
                            break;

                        case TypeKind.EntityType:
                            AddEntityHandler(
                                classBuilder,
                                constructorBuilder,
                                methodBuilder,
                                complexTypeDescriptor,
                                processed,
                                isNonNullable);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

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
