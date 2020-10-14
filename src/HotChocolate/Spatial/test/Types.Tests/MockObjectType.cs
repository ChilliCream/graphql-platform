using System;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public class MockObjectType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field<Resolvers>(x => x.FieldResolver()).Type<GeometryType>();
        }

        public class Resolvers
        {
            public Geometry FieldResolver() => throw new Exception();
        }
    }
}
