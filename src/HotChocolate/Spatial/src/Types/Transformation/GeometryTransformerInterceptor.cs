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
    /// <summary>
    /// Adds the <see cref="GeometryTransformerInputFormatter"/> and the
    /// <see cref="GeometryTransformationMiddleware"/> to the schema
    ///
    /// </summary>
    internal class GeometryTransformerInterceptor : TypeInterceptor
    {
        /// <inheritdoc />
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            ISpatialConvention convention = completionContext.GetSpatialConvention();
            if (convention.TransformerFactory.HasCoordinateSystems() &&
                convention.DefaultSrid > 0)
            {
                if (!convention.TransformerFactory.ContainsCoordinateSystem(convention.DefaultSrid))
                {
                    throw ThrowHelper.Transformation_DefaultCRSNotFound(convention.DefaultSrid);
                }

                switch (definition)
                {
                    case ObjectTypeDefinition def:
                        HandleObjectType(completionContext, def, convention);
                        break;
                    case InputObjectTypeDefinition def:
                        HandleInputObjectType(completionContext, def, convention);
                        break;
                    case DirectiveTypeDefinition def:
                        HandleDirectiveType(completionContext, def, convention);
                        break;
                }
            }
        }

        private static void HandleInputObjectType(
            ITypeCompletionContext completionContext,
            InputObjectTypeDefinition definition,
            ISpatialConvention convention)
        {
            foreach (var field in definition.Fields)
            {
                if (field.Type is not null &&
                    completionContext.IsNamedType<IGeoJsonInputType>(field.Type))
                {
                    field.Formatters.Add(
                        new GeometryTransformerInputFormatter(
                            convention.TransformerFactory,
                            convention.DefaultSrid));
                }
            }
        }

        private static void HandleDirectiveType(
            ITypeCompletionContext completionContext,
            DirectiveTypeDefinition definition,
            ISpatialConvention convention)
        {
            foreach (var arg in definition.Arguments)
            {
                if (arg.Type is not null &&
                    completionContext.IsNamedType<IGeoJsonInputType>(arg.Type))
                {
                    arg.Formatters.Add(
                        new GeometryTransformerInputFormatter(
                            convention.TransformerFactory,
                            convention.DefaultSrid));
                }
            }
        }

        private static void HandleObjectType(
            ITypeCompletionContext completionContext,
            ObjectTypeDefinition definition,
            ISpatialConvention convention)
        {
            foreach (var field in definition.Fields)
            {
                foreach (var arg in field.Arguments)
                {
                    if (arg.Type is not null &&
                        completionContext.IsNamedType<IGeoJsonInputType>(arg.Type))
                    {
                        arg.Formatters.Add(
                            new GeometryTransformerInputFormatter(
                                convention.TransformerFactory,
                                convention.DefaultSrid));
                    }
                }

                if (field.Type is not null &&
                    completionContext.IsNamedType<IGeoJsonObjectType>(field.Type))
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
                        FieldClassMiddlewareFactory.Create<GeometryTransformationMiddleware>(
                            (typeof(IGeometryTransformerFactory), convention.TransformerFactory),
                            (typeof(int), convention.DefaultSrid)));
                }
            }
        }
    }
}
