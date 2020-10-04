using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Spatial
{
    /// <summary>
    /// We deactivate description inference because we target multiple frameworks and it only
    /// works with 2.1
    /// </summary>
    public class MockNamingConvention : DefaultNamingConventions
    {
        public override string? GetTypeDescription(Type type, TypeKind kind)
        {
            return null;
        }
    }
}
