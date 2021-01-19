using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Spatial.Configuration;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Transformation
{
    internal class GeometryReprojectionInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            ISpatialConvention convention = completionContext.GetSpatialConvention();
            if (convention.ProjectorFactory.HasCoordinateSystems())
            {
                if (!convention.ProjectorFactory.ContainsCoordinateSystem(convention.DefaultSrid))
                {
                    throw ThrowHelper.Transformation_DefaultCRSNotFound(convention.DefaultSrid);
                }

                if (definition is ObjectTypeDefinition objectTypeDefinition)
                {
                    foreach (var field in objectTypeDefinition.Fields)
                    {
                        foreach (var arg in field.Arguments)
                        {
                            if (completionContext.IsType<IGeoJsonInputType>(arg.Type))
                            {
                                arg.Formatters.Add(
                                    new GeometryReprojectionInputFormatter(
                                        convention.ProjectorFactory,
                                        convention.DefaultSrid));
                            }
                        }

                        if (completionContext.IsType<IGeoJsonObjectType>(field.Type))
                        {
                            var argument =
                                ArgumentDescriptor.New(
                                    completionContext.DescriptorContext,
                                    CrsFieldName);

                            argument
                                .Type<IntType>()
                                .Description(Transformation_Argument_Crs_Description);

                            field.Arguments.Add(argument.CreateDefinition());
                            field.MiddlewareComponents.Insert(0,
                                FieldClassMiddlewareFactory.Create<ReprojectionMiddleware>((
                                    typeof(IGeometryProjectorFactory),
                                    convention.ProjectorFactory)));
                        }
                    }
                }

                if (definition is InputObjectTypeDefinition inputDefinition)
                {
                    foreach (var field in inputDefinition.Fields)
                    {
                        if (completionContext.IsType<IGeoJsonInputType>(field.Type))
                        {
                            field.Formatters.Add(
                                new GeometryReprojectionInputFormatter(
                                    convention.ProjectorFactory,
                                    convention.DefaultSrid));
                        }
                    }
                }
            }
        }
    }
}
