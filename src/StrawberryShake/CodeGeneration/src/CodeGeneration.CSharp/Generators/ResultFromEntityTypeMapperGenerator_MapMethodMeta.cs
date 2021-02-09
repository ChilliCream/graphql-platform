using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class ResultFromEntityTypeMapperGenerator
    {
        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        private void AddRequiredMapMethods(
            string propAccess,
            NamedTypeDescriptor namedTypeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            HashSet<string> processed)
        {
            if (namedTypeDescriptor.IsInterface)
            {
                foreach (var @class in namedTypeDescriptor.ImplementedBy)
                {
                    AddRequiredMapMethods(
                        propAccess,
                        @class,
                        classBuilder,
                        constructorBuilder,
                        processed);
                }
            }
            else
            {
                foreach (var property in namedTypeDescriptor.Properties)
                {
                    AddMapMethod(
                        propAccess,
                        property.Type,
                        classBuilder,
                        constructorBuilder,
                        processed);

                    if (property.Type.NamedType() is NamedTypeDescriptor nt &&
                        !nt.IsLeafType())
                    {
                        AddRequiredMapMethods(
                            propAccess,
                            nt,
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
            string propertyName = _entityParamName,
            bool isNonNullType = false)
        {
            var ifBuilder = IfBuilder
                .New()
                .SetCondition(
                    ConditionBuilder.New()
                        .Set($"{propertyName} == null"));
            ifBuilder.AddCode(
                isNonNullType
                    ? $"throw new {TypeNames.ArgumentNullException}();"
                    : "return null;");

            var codeBuilder = CodeBlockBuilder.New()
                .AddCode(ifBuilder)
                .AddEmptyLine();

            return codeBuilder;
        }

        private ICode BuildMapMethodCall(string objectName, PropertyDescriptor property)
        {
            switch (property.Type.Kind)
            {
                case TypeKind.LeafType:
                    return CodeInlineBuilder.New().SetText($"{objectName}.{property.Name}");
                case TypeKind.DataType:
                case TypeKind.EntityType:
                    var mapperMethodCall =
                        MethodCallBuilder
                            .New()
                            .SetDetermineStatement(false)
                            .SetMethodName(MapMethodNameFromTypeName(property.Type));

                    if (!property.Type.IsLeafType())
                    {
                        mapperMethodCall.AddArgument($"{objectName}.{property.Name}");
                    }

                    return mapperMethodCall;;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private MethodCallBuilder BuildMapMethodCall(ITypeDescriptor property, string firstArg)
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
            HashSet<string> processed)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddArrayHandler(
                        typeDescriptor,
                        classBuilder,
                        constructorBuilder,
                        methodBuilder,
                        listTypeDescriptor,
                        processed);
                    break;

                case NamedTypeDescriptor namedTypeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.LeafType:
                            break;

                        case TypeKind.DataType:
                            AddDataHandler(
                                typeDescriptor,
                                classBuilder,
                                constructorBuilder,
                                methodBuilder,
                                namedTypeDescriptor,
                                processed);
                            break;

                        case TypeKind.EntityType:
                            AddEntityHandler(
                                typeDescriptor,
                                classBuilder,
                                constructorBuilder,
                                methodBuilder,
                                namedTypeDescriptor,
                                processed);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }
    }
}
