using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGenerator : ClassBaseGenerator<TypeDescriptor>
    {
        const string EntityParamName = "entity";
        const string StoreFieldName = "_entityStore";
        const string MapMethodName = "Map";

        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor descriptor)
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
                StoreFieldName);

            // Define map method
            MethodBuilder mapMethod = MethodBuilder.New()
                .SetName(MapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            descriptor.Kind == TypeKind.EntityType
                                ? EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName)
                                : DataTypeNameFromTypeName(descriptor.Name))
                        .SetName(EntityParamName));

            var constructorCall = new MethodCallBuilder()
                .SetMethodName($"return new {descriptor.Name}");

            foreach (NamedTypeReferenceDescriptor propertyDescriptor in descriptor.Properties)
            {
                if (propertyDescriptor.Type.IsScalarType)
                {
                    constructorCall.AddArgument(EntityParamName + "." + propertyDescriptor.Name);
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
            NamedTypeReferenceDescriptor propertyDescriptor)
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
                            $"var {loopItem} in {EntityParamName}.{propertyDescriptor.Name}");

                    switch (listTypeDescriptor.InnerType)
                    {
                        case ListTypeDescriptor listTypeDescriptor1:
                            throw new NotImplementedException();

                        case TypeDescriptor typeDescriptor:
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

                case TypeDescriptor typeDescriptor:
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
            string variableName,
            string mappingArgument,
            TypeDescriptor typeDescriptor)
        {
            switch (typeDescriptor.Kind)
            {
                case TypeKind.Scalar:
                    throw new ArgumentException();
                case TypeKind.DataType:
                    var dataMapperName =
                        NamingConventions.DataMapperNameFromGraphQLTypeName(
                            typeDescriptor.Name,
                            typeDescriptor.GraphQLTypeName);

                    var dataMapperType =
                        $"IEntityMapper<" +
                        $"{DataTypeNameFromTypeName(typeDescriptor.Name)}, " +
                        $"{typeDescriptor.Name}>";

                    var dataMapperField = dataMapperName.ToFieldName();

                    var dataMapperCall = MappingCall(
                        dataMapperType,
                        dataMapperName,
                        dataMapperField,
                        $"{EntityParamName}.{mappingArgument}");

                    var dataItemVariable = $"{mappingArgument.WithLowerFirstChar()}";
                    method.AddCode(
                        AssignmentBuilder.New()
                            .SetLefthandSide($"var {dataItemVariable}")
                            .SetRighthandSide(dataMapperCall));
                    method.AddEmptyLine();
                    break;

                case TypeKind.EntityType:
                    if (typeDescriptor.IsInterface)
                    {
                        MapInterface(
                            method,
                            variableName,
                            mappingArgument,
                            typeDescriptor);
                    }
                    else
                    {
                        MapConcreteType(
                            method,
                            variableName,
                            mappingArgument,
                            typeDescriptor);
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
            TypeDescriptor typeDescriptor)
        {
            method.AddCode($"{typeDescriptor.Name} {variableName} = default!;");
            method.AddEmptyLine();

            var ifChain = InterfaceImplementeeIf(typeDescriptor.IsImplementedBy[0]);

            foreach (TypeDescriptor interfaceImplementee in typeDescriptor.IsImplementedBy.Skip(1))
            {
                var singleIf = InterfaceImplementeeIf(interfaceImplementee).SkipIndents();
                ifChain.AddIfElse(singleIf);
            }

            ifChain.AddElse(CodeInlineBuilder.New().SetText("throw new NotSupportedException();"));

            method.AddCode(ifChain);

            IfBuilder InterfaceImplementeeIf(TypeDescriptor interfaceImplementee)
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
            TypeDescriptor typeDescriptor,
            bool createNewVar = true)
        {
            var entityMapperName =
                NamingConventions.EntityMapperNameFromGraphQLTypeName(
                    typeDescriptor.Name,
                    typeDescriptor.GraphQLTypeName);
            
            var entityMapperType =
                $"IEntityMapper<" +
                $"{NamingConventions.EntityTypeNameFromGraphQLTypeName(typeDescriptor.Name)}, " +
                $"{typeDescriptor.Name}>";

            var entityMapperField = entityMapperName.ToFieldName();

            var mappingArgument = typeDescriptor.Kind == TypeKind.EntityType
                ? $"{StoreFieldName}.GetEntity<" +
                    $"{EntityTypeNameFromGraphQLTypeName(typeDescriptor.GraphQLTypeName)}" + 
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
                .SetMethodName(mapperFieldName + "." +MapMethodName)
                .SetDetermineStatement(false)
                .AddArgument(mappingArgumentName);

            return mapCallMethod;
        }
    }
}
