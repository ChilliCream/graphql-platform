using System;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

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
                .SetStatic()
                .SetName(descriptor.Name);

            MethodBuilder methodBuilder = MethodBuilder.New()
                .SetName(MapMethodName)
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.ResultType.Name)
                .SetStatic()
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(TypeBuilder.New().SetName(descriptor.EntityType.Name))
                        .SetName(EntityParamName)
                )
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(TypeBuilder.New().SetName(WellKnownTypes.EntityStore))
                        .SetName(StoreParamName)
                )
                .AddInlineCode($"return new ");

            var methodCallBuilder = new MethodCallBuilder()
                .SetMethodName(descriptor.ResultType.Name);
            foreach (TypeClassPropertyDescriptor propertyDescriptor in descriptor.ResultType.Properties)
            {
                if (propertyDescriptor.Type.IsReferenceType)
                {
                    MethodCallBuilder entityMapperMethod;
                    if (propertyDescriptor.Type.ListType == ListType.NoList)
                    {
                        entityMapperMethod = MethodCallBuilder.New()
                            .SetDetermineStatement(false)
                            .SetMethodName(
                                NamingConventions.MapperNameFromTypeName(propertyDescriptor.Type.Name) + "." +
                                MapMethodName
                            );

                        var entityGetterMethod = new MethodCallBuilder()
                            .SetMethodName(
                                StoreParamName
                                + "."
                                + nameof(IEntityStore.GetEntity)
                                + "<" + NamingConventions.EntityTypeNameFromTypeName(propertyDescriptor.Type.Name) + ">"
                            );
                        entityGetterMethod
                            .SetDetermineStatement(false)
                            .AddArgument(EntityParamName + "." + propertyDescriptor.Name);

                        entityMapperMethod.AddArgument(entityGetterMethod);
                        entityMapperMethod.AddArgument(StoreParamName);
                    }
                    else
                    {
                        var referencedEntity = "referencedEntity";

                        var mapCallMethod = new MethodCallBuilder()
                            .SetMethodName(
                                NamingConventions.MapperNameFromTypeName(propertyDescriptor.Type.Name) + "." +
                                MapMethodName
                            )
                            .SetDetermineStatement(false)
                            .AddArgument(referencedEntity)
                            .AddArgument(StoreParamName);

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
                                (propertyDescriptor.Type.ListType != ListType.NoList
                                    ? nameof(IEntityStore.GetEntities)
                                    : nameof(IEntityStore.GetEntity))
                                + "<" + NamingConventions.EntityTypeNameFromTypeName(propertyDescriptor.Type.Name) + ">")
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
            return classBuilder.BuildAsync(writer);
        }
    }
}
