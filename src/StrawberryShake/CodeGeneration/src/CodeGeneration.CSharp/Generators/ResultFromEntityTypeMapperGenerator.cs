using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGenerator : ClassBaseGenerator<ITypeDescriptor>
    {
        const string _entityParamName = "entity";
        const string _storeFieldName = "_entityStore";
        const string _mapMethodName = "Map";

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.EntityType && !descriptor.IsInterface();
        }

        protected override void Generate(CodeWriter writer, ITypeDescriptor typeDescriptor)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            NamedTypeDescriptor descriptor = typeDescriptor switch
            {
                NamedTypeDescriptor nullableNamedType => nullableNamedType,
                NonNullTypeDescriptor {InnerType: NamedTypeDescriptor namedType} => namedType,
                _ => throw new ArgumentException(nameof(typeDescriptor))
            };

            // Setup class
            classBuilder
                .AddImplements(
                    $"{TypeNames.IEntityMapper}<" +
                    (typeDescriptor.IsEntityType()
                        ? EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName)
                        : DataTypeNameFromTypeName(descriptor.Name)) +
                    $", {descriptor.Name}>")
                .SetName(
                    descriptor.Kind == TypeKind.EntityType
                        ? EntityMapperNameFromGraphQLTypeName(
                            descriptor.Name,
                            descriptor.GraphQLTypeName)
                        : DataMapperNameFromGraphQLTypeName(
                            descriptor.Name,
                            descriptor.GraphQLTypeName));

            constructorBuilder.SetTypeName(descriptor.Name);

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _storeFieldName,
                classBuilder,
                constructorBuilder);

            // Define map method
            MethodBuilder mapMethod = MethodBuilder.New()
                .SetName(_mapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            descriptor.Kind == TypeKind.EntityType
                                ? EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName)
                                : DataTypeNameFromTypeName(descriptor.Name))
                        .SetName(_entityParamName));

            var constructorCall = new MethodCallBuilder()
                .SetMethodName($"return new {descriptor.Name}");

            foreach (PropertyDescriptor propertyDescriptor in descriptor.Properties)
            {
                if (propertyDescriptor.Type.IsLeafType())
                {
                    constructorCall.AddArgument(_entityParamName + "." + propertyDescriptor.Name);
                }
                else
                {
                    TypeMapper(
                        mapMethod,
                        constructorCall,
                        propertyDescriptor,
                        propertyDescriptor.Type,
                        classBuilder,
                        constructorBuilder);
                }
            }

            mapMethod.AddCode(constructorCall);
            classBuilder.AddMethod(mapMethod);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private void TypeMapper<T>(
            ICodeContainer<T> mapMethod,
            MethodCallBuilder constructorCall,
            PropertyDescriptor propertyDescriptor,
            ITypeDescriptor typeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            var mappedType = propertyDescriptor.Name.WithLowerFirstChar();
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    var listVar = propertyDescriptor.Name.WithLowerFirstChar();

                    mapMethod.AddCode(
                        AssignmentBuilder.New()
                            .SetLefthandSide($"var {listVar}")
                            .SetRighthandSide($"new List<{propertyDescriptor.Type.Name}>()"));

                    var loopItem = $"{listVar}Item";
                    var foreachBuilder = ForEachBuilder.New()
                        .SetLoopHeader(
                            $"var {loopItem} in {_entityParamName}.{propertyDescriptor.Name}");

                    var mappedItemName = "mappedItem";
                    MapTypeDescriptor(
                        foreachBuilder,
                        mappedItemName,
                        loopItem,
                        listTypeDescriptor.InnerType,
                        classBuilder,
                        constructorBuilder);

                    foreachBuilder.AddEmptyLine();
                    foreachBuilder.AddCode($"{listVar}.Add({mappedItemName});");

                    mapMethod.AddCode(foreachBuilder);
                    mapMethod.AddEmptyLine();
                    constructorCall.AddArgument(listVar);
                    break;

                case NamedTypeDescriptor namedTypeDescriptor:
                    MapTypeDescriptor(
                        mapMethod,
                        mappedType,
                        propertyDescriptor.Name,
                        namedTypeDescriptor,
                        classBuilder,
                        constructorBuilder);

                    constructorCall.AddArgument(mappedType);
                    break;

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    TypeMapper(
                        mapMethod,
                        constructorCall,
                        propertyDescriptor,
                        nonNullTypeDescriptor.InnerType,
                        classBuilder,
                        constructorBuilder);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MapTypeDescriptor<T>(
            ICodeContainer<T> method,
            string variableName,
            string mappingArgument,
            ITypeDescriptor typeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    MapTypeDescriptor(
                        method,
                        variableName,
                        mappingArgument,
                        listTypeDescriptor.InnerType,
                        classBuilder,
                        constructorBuilder);

                    break;
                case NamedTypeDescriptor namedTypeDescriptor:
                    switch (namedTypeDescriptor.Kind)
                    {
                        case TypeKind.LeafType:
                            throw new ArgumentException();
                        case TypeKind.DataType:
                            var dataMapperName =
                                DataMapperNameFromGraphQLTypeName(
                                    namedTypeDescriptor.Name,
                                    namedTypeDescriptor.GraphQLTypeName);

                            var dataMapperType =
                                $"IEntityMapper<" +
                                $"{DataTypeNameFromTypeName(namedTypeDescriptor.Name)}, " +
                                $"{namedTypeDescriptor.Name}>";

                            var dataMapperField = dataMapperName.ToFieldName();

                            var dataMapperCall = MappingCall(
                                dataMapperType,
                                dataMapperName,
                                dataMapperField,
                                $"{_entityParamName}.{mappingArgument}",
                                classBuilder,
                                constructorBuilder);

                            var dataItemVariable = $"{mappingArgument.WithLowerFirstChar()}";
                            method.AddCode(
                                AssignmentBuilder.New()
                                    .SetLefthandSide($"var {dataItemVariable}")
                                    .SetRighthandSide(dataMapperCall));
                            method.AddEmptyLine();
                            break;

                        case TypeKind.EntityType:
                            if (namedTypeDescriptor.IsInterface)
                            {
                                MapInterface(
                                    method,
                                    variableName,
                                    mappingArgument,
                                    namedTypeDescriptor,
                                    classBuilder,
                                    constructorBuilder);
                            }
                            else
                            {
                                MapConcreteType(
                                    method,
                                    variableName,
                                    mappingArgument,
                                    namedTypeDescriptor,
                                    classBuilder,
                                    constructorBuilder);
                            }

                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    MapTypeDescriptor(
                        method,
                        variableName,
                        mappingArgument,
                        nonNullTypeDescriptor.InnerType,
                        classBuilder,
                        constructorBuilder);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }

        private void MapInterface<T>(
            ICodeContainer<T> method,
            string variableName,
            string mappingArgument,
            NamedTypeDescriptor namedTypeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            method.AddCode($"{namedTypeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            var ifChain = InterfaceImplementeeIf(namedTypeDescriptor.ImplementedBy[0]);

            foreach (NamedTypeDescriptor interfaceImplementee in
                namedTypeDescriptor.ImplementedBy.Skip(1))
            {
                var singleIf = InterfaceImplementeeIf(interfaceImplementee).SkipIndents();
                ifChain.AddIfElse(singleIf);
            }

            ifChain.AddElse(CodeInlineBuilder.New().SetText("throw new NotSupportedException();"));

            method.AddCode(ifChain);

            IfBuilder InterfaceImplementeeIf(NamedTypeDescriptor interfaceImplementee)
            {
                var ifCorrectType = IfBuilder.New()
                    .SetCondition(
                        $"{mappingArgument}.Name.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", StringComparison.Ordinal)");

                MapConcreteType(
                    ifCorrectType,
                    variableName,
                    mappingArgument,
                    interfaceImplementee,
                    classBuilder,
                    constructorBuilder,
                    false);

                return ifCorrectType;
            }
        }

        private void MapConcreteType<T>(
            ICodeContainer<T> method,
            string variableName,
            string argumentName,
            NamedTypeDescriptor namedTypeDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            bool createNewVar = true)
        {
            var entityMapperName =
                EntityMapperNameFromGraphQLTypeName(
                    namedTypeDescriptor.Name,
                    namedTypeDescriptor.GraphQLTypeName);

            var entityMapperType =
                $"IEntityMapper<" +
                $"{EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.Name)}, " +
                $"{namedTypeDescriptor.Name}>";

            var entityMapperField = entityMapperName.ToFieldName();

            var mappingArgument = namedTypeDescriptor.Kind == TypeKind.EntityType
                ? $"{_storeFieldName}.GetEntity<" +
                  $"{EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.GraphQLTypeName)}" +
                  $">({argumentName})"
                : argumentName;

            var entityMapperCall = MappingCall(
                entityMapperType,
                entityMapperName,
                entityMapperField,
                mappingArgument,
                classBuilder,
                constructorBuilder);

            method.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"{(createNewVar ? "var " : string.Empty)}{variableName}")
                    .SetRighthandSide(entityMapperCall));
            method.AddEmptyLine();
        }

        private MethodCallBuilder MappingCall(
            string mapperType,
            string mapperName,
            string mapperFieldName,
            string mappingArgumentName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            var mapperSet = new HashSet<string>();
            if (!mapperSet.Contains(mapperName))
            {
                mapperSet.Add(mapperName);

                AddConstructorAssignedField(
                    mapperType,
                    mapperFieldName,
                    classBuilder,
                    constructorBuilder);
            }

            var mapCallMethod = new MethodCallBuilder()
                .SetMethodName(mapperFieldName + "." + _mapMethodName)
                .SetDetermineStatement(false)
                .AddArgument(mappingArgumentName);

            return mapCallMethod;
        }
    }
}
