using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal interface ITypeConfigration
    {
        ConfigurationKind Kind { get; }

        IReadOnlyList<TypeDependency> Dependencies { get; }

        void Configure(ICompletionContext completionContext);
    }

    internal enum ConfigurationKind
    {
        Naming,
        Completion
    }
}
