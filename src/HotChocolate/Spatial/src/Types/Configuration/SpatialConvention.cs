using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Spatial.Transformation;
using NetTopologySuite;

namespace HotChocolate.Types.Spatial.Configuration
{
    public class SpatialConvention
        : Convention<SpatialConventionDefinition>
        , ISpatialConvention
    {
        private Action<ISpatialConventionDescriptor>? _configure;

        public SpatialConvention()
        {
            _configure = Configure;
        }

        public int DefaultSrid { get; private set; } = NtsGeometryServices.Instance.DefaultSRID;

        public IGeometryProjectorFactory ProjectorFactory { get; private set; }

        public SpatialConvention(Action<ISpatialConventionDescriptor> configure)
        {
            _configure = configure ??
                throw new ArgumentNullException(nameof(configure));
        }

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

        protected virtual void Configure(ISpatialConventionDescriptor descriptor)
        {
        }

        protected override void Complete(IConventionContext context)
        {
            if (Definition is null)
            {
                throw new InvalidOperationException();
            }

            DefaultSrid = Definition.DefaultSrid;
            ProjectorFactory = new GeometryProjectorFactory(Definition.CoordinateSystems);
        }
    }
}
