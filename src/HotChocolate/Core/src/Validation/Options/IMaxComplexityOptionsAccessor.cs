using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Options
{
    public interface IMaxComplexityOptionsAccessor
    {
        int DefaultComplexity { get; }

        int? MaxAllowedComplexity { get; }

        bool UseComplexityMultipliers { get; }

        ComplexityCalculation ComplexityCalculation { get; }
    }
}
