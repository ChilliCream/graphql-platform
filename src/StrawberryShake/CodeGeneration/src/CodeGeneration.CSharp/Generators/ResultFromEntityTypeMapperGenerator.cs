using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGenerator : ClassBaseGenerator<NamedTypeDescriptor>
    {
        const string _entityParamName = "entity";
        const string _storeFieldName = "_entityStore";
        const string _mapMethodName = "Map";

        protected override Task WriteAsync(CodeWriter writer, NamedTypeDescriptor descriptor)
        {
            AssertNonNull(
                writer,
                descriptor
            );

            // Setup class
            ClassBuilder
                .AddImplements(
                    "IEntityMapper<" +
                    $"{EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName)}, " +
                    $"{descriptor.Name}>")
                .SetName(
                    descriptor.Kind == TypeKind.EntityType
                        ? NamingConventions.EntityMapperNameFromGraphQLTypeName(
                            descriptor.Name,
                            descriptor.GraphQLTypeName)
                        : NamingConventions.DataMapperNameFromGraphQLTypeName(
                            descriptor.Name,
                            descriptor.GraphQLTypeName));

            ConstructorBuilder.SetTypeName(descriptor.Name);

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                _storeFieldName);

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
                        propertyDescriptor);
                }
            }

            mapMethod.AddCode(constructorCall);
            ClassBuilder.AddMethod(mapMethod);

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }

        private void TypeMapper(
            MethodBuilder mapMethod,
            MethodCallBuilder constructorCall,
            PropertyDescriptor propertyDescriptor)
        {
            switch (propertyDescriptor.Type)
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

                    switch (listTypeDescriptor.InnerType)
                    {
                        case ListTypeDescriptor listTypeDescriptor1:
                            throw new NotImplementedException();

                        case NamedTypeDescriptor typeDescriptor:
                            var mappedItemName = "mappedItem";
                            MapTypeDescriptor(
                                foreachBuilder,
                                mappedItemName,
                                loopItem,
                                typeDescriptor);
                            foreachBuilder.AddCode($"{listVar}.Add({mappedItemName});");
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    mapMethod.AddCode(foreachBuilder);
                    mapMethod.AddEmptyLine();
                    constructorCall.AddArgument(listVar);
                    break;

                case NamedTypeDescriptor typeDescriptor:
                    var mappedType = propertyDescriptor.Name.WithLowerFirstChar();
                    MapTypeDescriptor(
                        mapMethod,
                        mappedType,
                        propertyDescriptor.Name,
                        typeDescriptor);

                    constructorCall.AddArgument(mappedType);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MapTypeDescriptor<T>(
            ICodeContainer<T> method,
            NameString variableName,
            NameString mappingArgument,
            NamedTypeDescriptor namedTypeDescriptor)
        {
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
                        $"{_entityParamName}.{mappingArgument}");

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
                            namedTypeDescriptor);
                    }
                    else
                    {
                        MapConcreteType(
                            method,
                            variableName,
                            mappingArgument,
                            namedTypeDescriptor);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MapInterface<T>(
            ICodeContainer<T> method,
            string variableName,
            string mappingArgument,
            NamedTypeDescriptor namedTypeDescriptor)
        {
            method.AddCode($"{namedTypeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            var ifChain = InterfaceImplementeeIf(namedTypeDescriptor.ImplementedBy[0]);

            foreach (NamedTypeDescriptor interfaceImplementee in namedTypeDescriptor.ImplementedBy.Skip(1))
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
                    false);

                return ifCorrectType;
            }
        }

        private void MapConcreteType<T>(
            ICodeContainer<T> method,
            string variableName,
            string argumentName,
            NamedTypeDescriptor namedTypeDescriptor,
            bool createNewVar = true)
        {
            var entityMapperName =
                NamingConventions.EntityMapperNameFromGraphQLTypeName(
                    namedTypeDescriptor.Name,
                    namedTypeDescriptor.GraphQLTypeName);

            var entityMapperType =
                $"IEntityMapper<" +
                $"{NamingConventions.EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.Name)}, " +
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
                mappingArgument);

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
            string mappingArgumentName)
        {
            var mapperSet = new HashSet<string>();
            if (!mapperSet.Contains(mapperName))
            {
                mapperSet.Add(mapperName);

                AddConstructorAssignedField(
                    mapperType,
                    mapperFieldName);
            }

            var mapCallMethod = new MethodCallBuilder()
                .SetMethodName(mapperFieldName + "." +_mapMethodName)
                .SetDetermineStatement(false)
                .AddArgument(mappingArgumentName);

            return mapCallMethod;
        }
    }
}
