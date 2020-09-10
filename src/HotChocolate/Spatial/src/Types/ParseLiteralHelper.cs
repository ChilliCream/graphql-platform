using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Spatial
{
    public static class ParseLiteralHelper
    {
        public static (int typeIndex, int coordinateIndex, int crsIndex) GetFieldIndices(
            ObjectValueNode obj,
            string _typeFieldName,
            string _coordinatesFieldName,
            string _crsFieldName)
        {
            var coordinateIndex = -1;
            var typeIndex = -1;
            var crsIndex = -1;

            for (var i = 0; i < obj.Fields.Count; i++)
            {
                if (coordinateIndex > -1 && typeIndex > -1 && crsIndex > -1) {
                    return (typeIndex, coordinateIndex, crsIndex);
                }

                ObjectFieldNode field = obj.Fields[i];

                if (typeIndex < 0 && string.Equals(field.Name.Value, _typeFieldName,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    typeIndex = i;
                    continue;
                }

                if (coordinateIndex < 0 && string.Equals(field.Name.Value, _coordinatesFieldName,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    coordinateIndex = i;
                    continue;
                }

                if (crsIndex < 0 && string.Equals(field.Name.Value, _crsFieldName,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    crsIndex = i;
                    continue;
                }
            }

            return (typeIndex, coordinateIndex, crsIndex);
        }
    }
}
