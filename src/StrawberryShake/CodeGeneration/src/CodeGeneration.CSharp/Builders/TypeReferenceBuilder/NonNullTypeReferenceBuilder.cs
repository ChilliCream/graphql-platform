using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class NonNullTypeReferenceBuilder : ITypeReferenceBuilder
    {
        private ITypeReferenceBuilder? _innerType;

        public static NonNullTypeReferenceBuilder New() => new();

        public NonNullTypeReferenceBuilder SetInnerType(ITypeReferenceBuilder innerType)
        {
            _innerType = innerType;
            return this;
        }

        public void Build(CodeWriter writer, HashSet<string>? builderContext = null)
        {
            if (_innerType is null)
            {
                throw new ArgumentNullException(nameof(_innerType));
            }

            _innerType.Build(writer, new HashSet<string> {WellKnownBuilderContextData.SkipNullabilityOnce});
        }
    }
}
