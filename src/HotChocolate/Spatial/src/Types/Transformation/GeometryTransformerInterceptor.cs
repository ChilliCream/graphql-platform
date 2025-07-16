using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Spatial.Configuration;
using static HotChocolate.Types.Spatial.Properties.Resources;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial.Transformation;

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
        TypeSystemConfiguration configuration)
    {
        var convention = completionContext.GetSpatialConvention();
        if (convention.TransformerFactory.HasCoordinateSystems()
            && convention.DefaultSrid > 0)
        {
            if (!convention.TransformerFactory.ContainsCoordinateSystem(convention.DefaultSrid))
            {
                throw ThrowHelper.Transformation_DefaultCRSNotFound(convention.DefaultSrid);
            }

            switch (configuration)
            {
                case ObjectTypeConfiguration def:
                    HandleObjectType(completionContext, def, convention);
                    break;
                case InputObjectTypeConfiguration def:
                    HandleInputObjectType(completionContext, def, convention);
                    break;
                case DirectiveTypeConfiguration def:
                    HandleDirectiveType(completionContext, def, convention);
                    break;
            }
        }
    }

    private static void HandleInputObjectType(
        ITypeCompletionContext completionContext,
        InputObjectTypeConfiguration definition,
        ISpatialConvention convention)
    {
        foreach (var field in definition.Fields)
        {
            if (field.Type is not null
                && completionContext.IsNamedType<IGeoJsonInputType>(field.Type))
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
        DirectiveTypeConfiguration definition,
        ISpatialConvention convention)
    {
        foreach (var arg in definition.Arguments)
        {
            if (arg.Type is not null
                && completionContext.IsNamedType<IGeoJsonInputType>(arg.Type))
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
        ObjectTypeConfiguration definition,
        ISpatialConvention convention)
    {
        foreach (var field in definition.Fields)
        {
            foreach (var arg in field.Arguments)
            {
                if (arg.Type is not null
                    && completionContext.IsNamedType<IGeoJsonInputType>(arg.Type))
                {
                    arg.Formatters.Add(
                        new GeometryTransformerInputFormatter(
                            convention.TransformerFactory,
                            convention.DefaultSrid));
                }
            }

            if (field.Type is not null
                && completionContext.IsNamedType<IGeoJsonObjectType>(field.Type))
            {
                var argument =
                    ArgumentDescriptor.New(
                        completionContext.DescriptorContext,
                        CrsFieldName);

                argument
                    .Type<IntType>()
                    .Description(Transformation_Argument_Crs_Description);

                field.Arguments.Add(argument.CreateConfiguration());
                field.MiddlewareConfigurations.Insert(0,
                    new(FieldClassMiddlewareFactory.Create<GeometryTransformationMiddleware>(
                        (typeof(IGeometryTransformerFactory), convention.TransformerFactory),
                        (typeof(int), convention.DefaultSrid))));
            }
        }
    }
}
