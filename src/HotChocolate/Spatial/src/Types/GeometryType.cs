using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using NetTopologySuite.Geometries;
using static HotChocolate.Types.Spatial.WellKnownFields;

namespace HotChocolate.Types.Spatial
{
    public class GeometryType : ScalarType<Geometry>
    {
        private static Type[]? _inputs =
        {
             typeof(GeoJsonPointInput)
        };

        private static IReadOnlyList<TypeDependency>? _typeDependencies;
        private static ITypeReference? _geometryKindRef;

        private static IReadOnlyDictionary<GeoJsonGeometryType, IGeometryInputType> _inputTypes = null!;
        private static IReadOnlyDictionary<GeoJsonGeometryType, IGeometryType> _outTypes = null!;

        private static EnumType<GeoJsonGeometryType> _geometryKindType = null!;

        public GeometryType() : base("Geometry")
        {
        //    Description = Resources.GeoJsonPositionScalar_Description;
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            IDictionary<string, object?> contextData)
        {
            foreach (var type in _inputs!)
            {
                context.RegisterDependency(
                    new TypeDependency(
                        context.TypeInspector.GetTypeRef(
                            type,
                            TypeContext.Input,
                            context.Scope)));
            }

            _typeDependencies = context.TypeDependencies;

            _geometryKindRef = context.TypeInspector.GetTypeRef(
                typeof(EnumType<GeoJsonGeometryType>),
                TypeContext.None,
                context.Scope);

            context.RegisterDependency(new TypeDependency(_geometryKindRef));


            base.OnRegisterDependencies(context, contextData);
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            IDictionary<string, object?> contextData)
        {
            var inputTypes = new Dictionary<GeoJsonGeometryType, IGeometryInputType>();
            var outputTypes = new Dictionary<GeoJsonGeometryType, IGeometryType>();
            foreach (var dependency in _typeDependencies!)
            {
                if (context.TryGetType<IGeometryType>(
                    dependency.TypeReference,
                    out var geometryType))
                {
                    if (geometryType is IGeometryInputType inputType)
                    {
                        inputTypes[geometryType.GeometryType] = inputType;
                    }
                    else
                    {
                        outputTypes[geometryType.GeometryType] = geometryType;

                    }
                }
            }

            if (context.TryGetType<EnumType<GeoJsonGeometryType>>(
                _geometryKindRef!,
                out var kind))
            {
                _geometryKindType = kind;
            }
            else
            {
                throw new Exception();
            }

            _inputTypes = inputTypes;
            _outTypes = outputTypes;
            _inputs = null!;
            _geometryKindRef = null!;
            _typeDependencies = null!;

            base.OnCompleteType(context, contextData);
        }

        public GeometryType(
            NameString name,
            BindingBehavior bind = BindingBehavior.Explicit) : base(name, bind)
        {
        }

        // Null or Runtime Dictionary<string, object> or List<object>, => Null or Runtime Value
        public override object? Deserialize(object? resultValue)
        {

            return base.Deserialize(resultValue);
        }

        // Null or Runtime => Null or Dictionary<string, object> or List<object> or ResultMap or ResultMapList or ResultValueList
        public override object? Serialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (!(runtimeValue is Geometry geometry))
            {
                throw new Exception();
            }

            if (!TryGetGeometryKind(geometry, out GeoJsonGeometryType kind))
            {
                throw new Exception();
            }

            if (!_outTypes.TryGetValue(kind, out IGeometryType? geometryType))
            {
                throw new Exception();
            }
            //TODO: how should we hook into the execution engine?

            throw new Exception();
        }

        public override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            IGeometryInputType geometryType = GetGeometryType(valueSyntax);
            return geometryType.IsInstanceOfType(valueSyntax);
        }

        public override object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            IGeometryInputType geometryType = GetGeometryType(valueSyntax);
            return geometryType.ParseLiteral(valueSyntax);
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (!(runtimeValue is Geometry geometry))
            {
                throw new Exception();
            }

            if (!TryGetGeometryKind(geometry, out GeoJsonGeometryType kind))
            {
                throw new Exception();
            }

            if (!_inputTypes.TryGetValue(kind, out IGeometryInputType? geometryType))
            {
                throw new Exception();
            }

            return geometryType.ParseValue(runtimeValue);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is Geometry)
            {
                return ParseValue(resultValue);
            }

            if (resultValue is IReadOnlyDictionary<string, object> d &&
                d.TryGetValue(TypeFieldName, out object? fieldObject) &&
                fieldObject is string field &&
                _geometryKindType.ParseLiteral(
                    new StringValueNode(field)) is GeoJsonGeometryType type &&
                _inputTypes.TryGetValue(type, out var geometryType))
            {
                return geometryType.ParseValue(resultValue);
            }

            throw new Exception();
        }

        private IGeometryInputType GetGeometryType(IValueNode valueSyntax)
        {
            valueSyntax.EnsureObjectValueNode(out var obj);

            if (!TryGetGeometryKind(obj, out GeoJsonGeometryType geometryType))
            {
                throw new Exception();
            }

            if (!_inputTypes.TryGetValue(geometryType, out IGeometryInputType? type))
            {
                throw new Exception();
            }

            return type;
        }

        private bool TryGetGeometryKind(
            Geometry geometry,
            out GeoJsonGeometryType geometryType)
        {
            if (_geometryKindType.ParseLiteral(
                new StringValueNode(geometry.GeometryType)) is GeoJsonGeometryType type)
            {
                geometryType = type;
                return true;
            }

            geometryType = default;
            return false;
        }

        private bool TryGetGeometryKind(
            ObjectValueNode valueSyntax,
            out GeoJsonGeometryType geometryType)
        {
            IReadOnlyList<ObjectFieldNode> fields = valueSyntax.Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name.Value == TypeFieldName &&
                    _geometryKindType.ParseLiteral(fields[i].Value) is GeoJsonGeometryType type)
                {
                    geometryType = type;
                    return true;
                }
            }

            geometryType = default;
            return false;
        }
    }
}
