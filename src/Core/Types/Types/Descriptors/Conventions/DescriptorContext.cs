using System;

namespace HotChocolate.Types.Descriptors
{
    public sealed class DescriptorContext
        : IDescriptorContext
    {
        private DescriptorContext(
            INamingConventions naming,
            ITypeInspector inspector)
        {
            if (naming == null)
            {
                throw new ArgumentNullException(nameof(naming));
            }

            if (inspector == null)
            {
                throw new ArgumentNullException(nameof(inspector));
            }

            Naming = naming;
            Inspector = inspector;
        }

        public INamingConventions Naming { get; }

        public ITypeInspector Inspector { get; }

        public static DescriptorContext Create(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var naming =
                (INamingConventions)services.GetService(
                    typeof(INamingConventions));
            if (naming == null)
            {
                naming = new DefaultNamingConventions();
            }

            var inspector =
                (ITypeInspector)services.GetService(
                    typeof(ITypeInspector));
            if (inspector == null)
            {
                inspector = new DefaultTypeInspector();
            }

            return new DescriptorContext(naming, inspector);
        }

        public static DescriptorContext Create()
        {
            return new DescriptorContext(
                new DefaultNamingConventions(),
                new DefaultTypeInspector());
        }
    }
}
