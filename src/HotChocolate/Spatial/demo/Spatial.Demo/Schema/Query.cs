using System.Linq;
using HotChocolate;
using HotChocolate.Data;

namespace Spatial.Demo
{
    public class Query
    {
        [UseDbContext(typeof(ApplicationDbContext))]
        [UseProjection]
        [UseFiltering]
        public IQueryable<Parcel> GetParcels(
            [ScopedService] ApplicationDbContext context) =>
            context.Parcels;

        [UseDbContext(typeof(ApplicationDbContext))]
        [UseProjection]
        [UseFiltering]
        public IQueryable<LiquorStore> GetLiquorStores(
            [ScopedService] ApplicationDbContext context) =>
            context.LiquorStores;

        [UseDbContext(typeof(ApplicationDbContext))]
        [UseProjection]
        [UseFiltering]
        public IQueryable<GolfCourse> GetGolfCourses(
            [ScopedService] ApplicationDbContext context) =>
            context.GolfCourses;
    }
}
