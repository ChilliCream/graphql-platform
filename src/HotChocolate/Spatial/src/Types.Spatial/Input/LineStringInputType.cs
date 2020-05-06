using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities.Serialization;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Types.Spatial.Input
{
    public class LineStringInputObject : InputObjectType<LineString>
    {
        private const string _points = "points";
        private const string _srid = "srid";

        private IInputField _pointsField = default!;
        private IInputField _sridField = default!;

        public LineStringInputObject() {}

        protected override void Configure(IInputObjectTypeDescriptor<LineString> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            // required fields
            descriptor.Field(_points)
                .Type<NonNullType<ListType<PointInputObject>>>()
                .Description("An array of points comprising the line. There must be more than 1 point");

            // optional fields
            descriptor.Field(_srid)
                .Type<IntType>()
                .Description("Spatial Reference System Identifier. e.g. latitude/longitude (WGS84): 4326, web mercator: 3867");
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return null;
            }

            if (!(literal is ObjectValueNode obj))
            {
                throw new InputObjectSerializationException(
                    "Failed to serialize LineStringInputObject. Needs at least two input fields");
            }

            int? srid = null;
            var points = new List<Point>();

            for (int i = 0; i < obj.Fields.Count; i++)
            {
                ObjectFieldNode field = obj.Fields[i];

                switch (field.Name.Value)
                {
                    case _points:
                        points = (List<Point>)_pointsField.Type.ParseLiteral(field.Value);
                        break;
                    case _srid:
                        srid = (int)_sridField.Type.ParseLiteral(field.Value);
                        break;
                }
            }

            if (points.Count < 2) {
                throw new InputObjectSerializationException(
                    "Failed to serialize LineStringInputObject. A line requires at least two points");
            }

            var coordinates = new Coordinate[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                coordinates[i] = new Coordinate(points[i].X, points[i].Y);
            }

            if (!srid.HasValue)
            {
                return new LineString(coordinates);
            }

            var factory = new NtsGeometryServices().CreateGeometryFactory(srid.Value);

            return factory.CreateLineString(coordinates);
        }

        public override bool TrySerialize(object value, out object? serialized)
        {
            return base.TrySerialize(value, out serialized);
        }

        public override bool TryDeserialize(object serialized, out object? value)
        {
            return base.TryDeserialize(serialized, out value);
        }

        protected override void OnAfterCompleteType(
            ICompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            _pointsField = Fields[_points];
            _sridField = Fields[_srid];
        }
    }
}
