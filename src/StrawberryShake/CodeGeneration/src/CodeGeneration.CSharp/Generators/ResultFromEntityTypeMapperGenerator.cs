using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultFromEntityTypeMapperGenerator : CodeGenerator<ResultFromEntityTypeMapperDescriptor>
    {
        const string EntityParamName = "entity";
        const string StoreParamName = "entityStore";
        const string MapMethodName = "Map";

        protected override Task WriteAsync(CodeWriter writer, ResultFromEntityTypeMapperDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            // Setup class
            ClassBuilder classBuilder = ClassBuilder.New()
                .AddImplements($"IEntityMapper<{descriptor.EntityType.Name}, {descriptor.ResultType.Name}>")
                .AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetName(StoreParamName.ToFieldName())
                        .SetType(WellKnownNames.EntityStore)
                )
                .SetName(descriptor.Name);

            var constructorBuilder = ConstructorBuilder
                .New()
                .SetTypeName(descriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(TypeReferenceBuilder.New().SetName(WellKnownNames.EntityStore))
                        .SetName(StoreParamName)
                )
                .AddCode(AssignmentBuilder.New()
                    .SetLefthandSide(StoreParamName.ToFieldName())
                    .SetRighthandSide(StoreParamName)
                    .AssertNonNull()
                );

            // Define map method
            MethodBuilder methodBuilder = MethodBuilder.New()
                .SetName(MapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.ResultType.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(TypeReferenceBuilder.New().SetName(descriptor.EntityType.Name))
                        .SetName(EntityParamName)
                );

            var methodCallBuilder = new MethodCallBuilder()
                .SetMethodName($"return new {descriptor.ResultType.Name}");

            var mapperSet = new HashSet<string>();

            foreach (TypePropertyDescriptor propertyDescriptor in descriptor.ResultType.Properties)
            {
                if (propertyDescriptor.TypeReference.IsReferenceType)
                {
                    var propertyMapperName = NamingConventions.MapperNameFromTypeName(propertyDescriptor.TypeReference.Name);
                    var propertyMapperTypeName =
                        $"IEntityMapper<{NamingConventions.EntityTypeNameFromTypeName(propertyDescriptor.TypeReference.Name)}, {propertyDescriptor.TypeReference.Name}>";
                    var propertyMapperFieldName = propertyMapperName.ToFieldName();
                    var propertyMapperParamName = propertyMapperName.WithLowerFirstChar();

                    if (!mapperSet.Contains(propertyMapperName))
                    {
                        mapperSet.Add(propertyMapperName);
                        classBuilder.AddField(
                            FieldBuilder.New()
                                .SetReadOnly()
                                .SetName(propertyMapperFieldName)
                                .SetType(propertyMapperTypeName)
                        );

                        constructorBuilder.AddParameter(
                            ParameterBuilder.New()
                                .SetName(propertyMapperParamName)
                                .SetType(propertyMapperTypeName)
                        );

                        constructorBuilder.AddCode(AssignmentBuilder.New()
                            .SetLefthandSide(propertyMapperFieldName)
                            .SetRighthandSide(propertyMapperParamName)
                            .AssertNonNull());
                    }


                    MethodCallBuilder entityMapperMethod;
                    if (propertyDescriptor.TypeReference.ListType == ListType.NoList)
                    {
                        entityMapperMethod = MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName(
                                propertyMapperFieldName + "." +
                                MapMethodName
                            );

                        var entityGetterMethod = new MethodCallBuilder()
                            .SetMethodName(
                                StoreParamName
                                + "."
                                + nameof(IEntityStore.GetEntity)
                                + "<" + NamingConventions.EntityTypeNameFromTypeName(propertyDescriptor.TypeReference.Name) + ">"
                            );
                        entityGetterMethod
                            .SetDetermineStatement(false)
                            .AddArgument(EntityParamName + "." + propertyDescriptor.Name);

                        entityMapperMethod.AddArgument(entityGetterMethod);
                    }
                    else
                    {
                        var referencedEntity = "referencedEntity";

                        var mapCallMethod = new MethodCallBuilder()
                            .SetMethodName(
                                propertyMapperFieldName + "." +
                                MapMethodName
                            )
                            .SetDetermineStatement(false)
                            .AddArgument(referencedEntity);

                        var mapLambda = new LambdaBuilder()
                            .AddArgument(referencedEntity)
                            .SetCode(mapCallMethod);

                        var selectMethod = new MethodCallBuilder()
                            .SetMethodName("Select")
                            .SetDetermineStatement(false)
                            .AddArgument(mapLambda);

                        var toListMethod = new MethodCallBuilder()
                            .SetDetermineStatement(false)
                            .SetMethodName("ToList");

                        entityMapperMethod = new MethodCallBuilder()
                            .SetMethodName(
                                StoreParamName + "." +
                                (propertyDescriptor.TypeReference.ListType != ListType.NoList
                                    ? nameof(IEntityStore.GetEntities)
                                    : nameof(IEntityStore.GetEntity))
                                + "<" + NamingConventions.EntityTypeNameFromTypeName(propertyDescriptor.TypeReference.Name) + ">"
                            )
                            .SetDetermineStatement(false)
                            .AddArgument(EntityParamName + "." + propertyDescriptor.Name)
                            .AddChainedCode(selectMethod)
                            .AddChainedCode(toListMethod);
                    }

                    methodCallBuilder.AddArgument(entityMapperMethod);
                }
                else
                {
                    methodCallBuilder.AddArgument(EntityParamName + "." + propertyDescriptor.Name);
                }
            }


            methodBuilder.AddCode(methodCallBuilder);
            classBuilder.AddMethod(methodBuilder);
            classBuilder.AddConstructor(constructorBuilder);
            return classBuilder.BuildAsync(writer);
        }
    }
}
