using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial;
using HotChocolate.Types.Spatial.Serialization;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extensions to the <see cref="IRequestExecutorBuilder"/>.
    /// </summary>
    public static class HotChocolateSpatialRequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="conventionFactory">
        /// Creates the convention for spatial types
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder,
            Func<SpatialConvention> conventionFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder
                .ConfigureSchema(x => x.AddSpatialTypes(conventionFactory));
        }

        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="descriptor">
        /// Configure the spatial convention
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(
            this IRequestExecutorBuilder builder,
            Action<ISpatialConventionDescriptor> descriptor)
        {
            return builder.AddSpatialTypes(() => new SpatialConvention(descriptor));
        }

        /// <summary>
        /// Adds GeoJSON compliant spatial types.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder AddSpatialTypes(this IRequestExecutorBuilder builder)
        {
            return builder.AddSpatialTypes(() =>
                new SpatialConvention(x => x.AddDefaultSerializers()));
        }
    }

    public static class SpatialDescriptorContextExtensions
    {
        public static ISpatialConvention GetSpatialConvention(
            this ITypeCompletionContext context) =>
            context.DescriptorContext.GetSpatialConvention();

        public static ISpatialConvention GetSpatialConvention(this IDescriptorContext context) =>
            context.GetConventionOrDefault<ISpatialConvention>(() =>
                throw new InvalidOperationException());
    }

    public class SpatialConventionDefinition
    {
        public int DefaultSrid { get; set; } = NtsGeometryServices.Instance.DefaultSRID;

        internal List<SpatialSerializerDefinition> Serializers { get; } = new();
    }

    public interface ISpatialConvention : IConvention
    {
        T GetSerializer<T>() where T : IGeoJsonSerializer;

        IGeoJsonSerializer GetSerializer(GeoJsonGeometryType kind);
    }

    public interface ISpatialConventionDescriptor
    {
        ISpatialConventionDescriptor DefaultSrid(int srid);

        ISpatialConventionDescriptor AddSerializer<TType, TSerializer>(
            GeoJsonGeometryType? kind = default)
            where TSerializer : IGeoJsonSerializer, new();
    }

    public class SpatialConventionDescriptor : ISpatialConventionDescriptor
    {
        protected SpatialConventionDefinition Definition { get; } = new();

        public SpatialConventionDefinition CreateDefinition()
        {
            return Definition;
        }

        public ISpatialConventionDescriptor DefaultSrid(int srid)
        {
            Definition.DefaultSrid = srid;
            return this;
        }

        public ISpatialConventionDescriptor AddSerializer<TType, TSerializer>(
            GeoJsonGeometryType? kind = default)
            where TSerializer : IGeoJsonSerializer, new()
        {
            Definition.Serializers.Add(
                new SpatialSerializerDefinition(
                    kind,
                    new TSerializer(),
                    typeof(TType)));

            return this;
        }

        public static SpatialConventionDescriptor New() => new();
    }

    public static class SpatialConventionDescriptorExtensions
    {
        public static ISpatialConventionDescriptor AddDefaultSerializers(
            this ISpatialConventionDescriptor descriptor)
        {
            return descriptor
                .AddSerializer<Coordinate, GeoJsonPositionSerializer>()
                .AddSerializer<Geometry, GeoJsonGeometrySerializer>()
                .AddSerializer<GeoJsonGeometryType, GeoJsonTypeSerializer>()
                .AddSerializer<Point, GeoJsonPointSerializer>(GeoJsonGeometryType.Point)
                .AddSerializer<MultiPoint, GeoJsonMultiPointSerializer>(
                    GeoJsonGeometryType.MultiPoint)
                .AddSerializer<LineString, GeoJsonLineStringSerializer>(
                    GeoJsonGeometryType.LineString)
                .AddSerializer<MultiLineString, GeoJsonMultiLineStringSerializer>(
                    GeoJsonGeometryType.MultiLineString)
                .AddSerializer<Polygon, GeoJsonPolygonSerializer>(
                    GeoJsonGeometryType.Polygon)
                .AddSerializer<MultiPolygon, GeoJsonMultiPolygonSerializer>(
                    GeoJsonGeometryType.MultiPolygon);
        }
    }

    public class SpatialConvention : Convention<SpatialConventionDefinition>, ISpatialConvention
    {
        private readonly Dictionary<Type, IGeoJsonSerializer> _serializerByType = new();
        private readonly Dictionary<GeoJsonGeometryType, IGeoJsonSerializer> _serializersByKind = new();


        private Action<ISpatialConventionDescriptor>? _configure;

        protected SpatialConvention()
        {
            _configure = Configure;
        }

        public SpatialConvention(Action<ISpatialConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        protected override SpatialConventionDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                // TODO Resource
                throw new InvalidOperationException();
            }

            var descriptor = SpatialConventionDescriptor.New();

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(ISpatialConventionDescriptor descriptor)
        {
        }

        protected override void Complete(IConventionContext context)
        {
            if (Definition is null)
            {
                throw new InvalidOperationException();
            }

            var provider =
                new SpatialSerializerProvider(Definition.DefaultSrid, Definition.Serializers);
            var serializerContext = new SpatialSerializerContext(this, provider);

            foreach (var definition in Definition.Serializers)
            {
                _serializerByType[definition.SerializerType] = definition.Serializer;
                if (definition.GeometryKind.HasValue)
                {
                    _serializersByKind[definition.GeometryKind.Value] = definition.Serializer;
                }
            }

            foreach (var definition in Definition.Serializers)
            {
                if (definition.Serializer is GeoJsonSerializerBase serializer)
                {
                    serializer.Initialize(serializerContext);
                }
            }
        }


        public T GetSerializer<T>() where T : IGeoJsonSerializer
        {
            if (_serializerByType.TryGetValue(typeof(T), out var serializerObj) &&
                serializerObj is T serializer)
            {
                return serializer;
            }

            // ToDo Throwhelper
            throw new InvalidOperationException();
        }

        public IGeoJsonSerializer GetSerializer(GeoJsonGeometryType kind)
        {
            if (_serializersByKind.TryGetValue(kind, out var serializer))
            {
                return serializer;
            }

            // ToDo Throwhelper
            throw new InvalidOperationException();
        }
    }
}
