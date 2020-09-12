using System.Collections.Generic;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public interface ILazyTypeConfiguration
    {
        /// <summary>
        /// Defines on which type initialization step this
        /// configurations is applied on.
        /// </summary>
        ApplyConfigurationOn On { get; }

        /// <summary>
        /// Defines types on on which this configuration is dependant on.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<TypeDependency> Dependencies { get; }

        /// <summary>
        /// Executes this configuration.
        /// </summary>
        void Configure(ITypeCompletionContext context);
    }
}
