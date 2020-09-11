using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial
{
    public abstract partial class GeoJSONInputObjectType<T>
        : InputObjectType<T>,
          IGeometryType
    {
        private IInputField _typeField = default!;
        private IInputField _coordinatesField = default!;
        private IInputField _crsField = default!;

        public abstract GeoJSONGeometryType GeometryType { get; }

        protected override void OnAfterCompleteType(
            ITypeCompletionContext context,
            DefinitionBase definition,
            IDictionary<string, object?> contextData)
        {
            _coordinatesField = Fields[CoordinatesFieldName];
            _typeField = Fields[TypeFieldName];
            _crsField = Fields[CrsFieldName];
        }
    }
}
