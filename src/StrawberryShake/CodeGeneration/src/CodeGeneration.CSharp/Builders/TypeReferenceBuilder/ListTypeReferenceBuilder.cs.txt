using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp.Builders
{
    public class ListTypeReferenceBuilder : ITypeReferenceBuilder
    {
        private ITypeReferenceBuilder? _innerType;

        public static ListTypeReferenceBuilder New() => new();

        public ListTypeReferenceBuilder SetListType(ITypeReferenceBuilder innerType)
        {
            _innerType = innerType;
            return this;
        }

        public void Build(CodeWriter writer)
        {
            if (_innerType is null)
            {
                throw new ArgumentNullException(nameof(_innerType));
            }

            writer.Write("IReadOnlyList<");
            _innerType.Build(writer);
            writer.Write(">");
            if (builderContext is null ||
                !builderContext.Contains(WellKnownBuilderContextData.SkipNullabilityOnce))
            {
                writer.Write("?");
            }
        }
    }
}
