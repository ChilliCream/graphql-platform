using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Transformation;
using NetTopologySuite;

namespace HotChocolate.Types.Spatial.Configuration
{
    /// <summary>
    /// The convention of the
    /// </summary>
    public class SpatialConvention
        : Convention<SpatialConventionDefinition>
        , ISpatialConvention
    {
        private Action<ISpatialConventionDescriptor>? _configure;

        /// <inheritdoc />
        public SpatialConvention()
        {
            _configure = Configure;
        }

        /// <inheritdoc />
        public int DefaultSrid { get; private set; } = NtsGeometryServices.Instance.DefaultSRID;

        /// <inheritdoc />
        public IGeometryTransformerFactory TransformerFactory { get; private set; } = default!;

        /// <inheritdoc />
        public SpatialConvention(Action<ISpatialConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

        /// <inheritdoc />
        protected override SpatialConventionDefinition CreateDefinition(IConventionContext context)
        {
            if (_configure is null)
            {
                throw new InvalidOperationException();
            }

            var descriptor = SpatialConventionDescriptor.New();

            _configure(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        /// <summary>
        /// This method is called on initialization of the convention but before the convention is
        /// completed. The default implementation of this method does nothing. It can be overriden
        /// by a derived class such that the convention can be further configured before it is
        /// completed
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor that can be used to configure the convention
        /// </param>
        protected virtual void Configure(ISpatialConventionDescriptor descriptor)
        {
        }

        /// <inheritdoc />
        protected override void Complete(IConventionContext context)
        {
            if (Definition is null)
            {
                throw new InvalidOperationException();
            }

            DefaultSrid = Definition.DefaultSrid;
            TransformerFactory = new GeometryTransformerFactory(Definition.CoordinateSystems);
        }
    }
}
