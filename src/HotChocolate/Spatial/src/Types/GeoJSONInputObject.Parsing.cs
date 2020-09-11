using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using NetTopologySuite.Geometries;

namespace HotChocolate.Types.Spatial
{
    public abstract partial class GeoJSONInputObjectType<T>
    {
        protected void ValidateGeometryKind(ObjectValueNode valueNode, int fieldIndex)
            => ParseGeometryKind(valueNode, fieldIndex);

        protected GeoJSONGeometryType ParseGeometryKind(
            ObjectValueNode valueNode,
            int fieldIndex)
        {
            if (fieldIndex == -1)
            {
                throw ThrowHelper.InvalidStructure_TypeIsMissing(this);
            }

            IValueNode typeValue = valueNode.Fields[fieldIndex].Value;

            if (!(_typeField.Type.ParseLiteral(typeValue) is GeoJSONGeometryType type))
            {
                throw ThrowHelper.InvalidStructure_TypeCannotBeNull(this);
            }

            if (type != GeometryType)
            {
                throw ThrowHelper.InvalidStructure_IsOfWrongGeometryType(type, this);
            }

            return type;
        }

        protected Coordinate ParsePoint(
            ObjectValueNode valueNode,
            int fieldIndex)
        {
            if (fieldIndex == -1)
            {
                throw ThrowHelper.InvalidStructure_CoordinatesIsMissing(this);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is Coordinate
                coordinates))
            {
                throw ThrowHelper.InvalidStructure_CoordinatesCannotBeNull(this);
            }

            return coordinates;
        }

        protected IList<Coordinate> ParseCoordinateValues(
            ObjectValueNode valueNode,
            int fieldIndex,
            int coordinateCount)
        {
            if (fieldIndex == -1)
            {
                throw ThrowHelper.InvalidStructure_CoordinatesIsMissing(this);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is IList<Coordinate>
                coordinates))
            {
                throw ThrowHelper.InvalidStructure_CoordinatesCannotBeNull(this);
            }

            if (coordinates.Count < coordinateCount)
            {
                throw ThrowHelper.InvalidStructure_CoordinatesOfWrongFormat(this);
            }

            return coordinates;
        }

        protected IList<List<Coordinate>> ParseCoordinateParts(
            ObjectValueNode valueNode,
            int fieldIndex,
            int partCount)
        {
            if (fieldIndex == -1)
            {
                throw ThrowHelper.InvalidStructure_CoordinatesIsMissing(this);
            }

            IValueNode coordinatesValue = valueNode.Fields[fieldIndex].Value;

            if (!(_coordinatesField.Type.ParseLiteral(coordinatesValue) is List<List<Coordinate>>
                coordinates))
            {
                throw ThrowHelper.InvalidStructure_CoordinatesCannotBeNull(this);
            }

            if (coordinates.Count < partCount)
            {
                throw ThrowHelper.InvalidStructure_CoordinatesOfWrongFormat(this);
            }

            return coordinates;
        }

        protected bool TryParseCrs(
            ObjectValueNode valueNode,
            int fieldIndex,
            out int srid)
        {
            if (fieldIndex > 0)
            {
                IValueNode crsField = valueNode.Fields[fieldIndex].Value;

                if (_crsField.Type.ParseLiteral(crsField) is int parsedSrid)
                {
                    srid = parsedSrid;
                    return true;
                }
            }

            srid = 0;
            return false;
        }

        protected (int typeIndex, int coordinateIndex, int crsIndex) GetFieldIndices(
            ObjectValueNode obj)
        {
            var coordinateIndex = -1;
            var typeIndex = -1;
            var crsIndex = -1;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                if (coordinateIndex > -1 && typeIndex > -1 && crsIndex > -1)
                {
                    break;
                }

                var fieldName = obj.Fields[i].Name.Value;

                if (typeIndex < 0 &&
                    WellKnownFields.TypeFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    typeIndex = i;
                }
                else if (coordinateIndex < 0 &&
                    WellKnownFields.CoordinatesFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    coordinateIndex = i;
                }
                else if (crsIndex < 0 &&
                    WellKnownFields.CrsFieldName.EqualsInvariantIgnoreCase(fieldName))
                {
                    crsIndex = i;
                }
            }

            return (typeIndex, coordinateIndex, crsIndex);
        }
    }
}
