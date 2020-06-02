using System; 
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Filters;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial.Filters
{
    public static class GeometryFilterTypeExtensions
    {
        public static IGeometryFilterFieldDescriptor Filter<T>(
            this IFilterInputTypeDescriptor<T> descriptor,
            Expression<Func<T, Geometry>> property)
        {
            if (property.ExtractMember() is PropertyInfo p)
            {
                return descriptor.AddFilter(
                    p,
                    ctx => new GeometryFilterFieldDescriptor(ctx, p, ctx.GetFilterConvention()));
            }

            throw new ArgumentException("Only properties allowed", nameof(property));
        }
    }
}