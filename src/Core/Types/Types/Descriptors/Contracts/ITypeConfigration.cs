using System.Collections.Generic;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    internal interface ITypeConfigration
    {
        ConfigurationKind Kind { get; }

        IReadOnlyList<TypeDependency> Dependencies { get; }

        void Configure(ICompletionContext completionContext);
    }
}
