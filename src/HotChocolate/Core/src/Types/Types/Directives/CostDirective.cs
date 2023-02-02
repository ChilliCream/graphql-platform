using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// The cost directive can be used to express the expected
/// cost that a resolver incurs on the system.
/// </summary>
public sealed class CostDirective
{
    public CostDirective()
    {
        Complexity = 1;
        Multipliers = Array.Empty<MultiplierPathString>();
    }

    public CostDirective(int complexity)
    {
        if (complexity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(complexity),
                complexity,
                TypeResources.CostDirective_ComplexityCannotBeBelowOne);
        }

        Complexity = complexity;
        Multipliers = Array.Empty<MultiplierPathString>();
    }

    public CostDirective(
        int complexity,
        params MultiplierPathString[] multipliers)
    {
        if (complexity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(complexity),
                complexity,
                TypeResources.CostDirective_ComplexityCannotBeBelowOne);
        }

        if (multipliers is null)
        {
            throw new ArgumentNullException(nameof(multipliers));
        }

        Complexity = complexity;
        Multipliers = multipliers.Where(t => t.HasValue).ToArray();
    }

    public CostDirective(
        int complexity,
        int defaultMultiplier,
        params MultiplierPathString[] multipliers)
    {
        if (complexity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(complexity),
                complexity,
                TypeResources.CostDirective_ComplexityCannotBeBelowOne);
        }

        if (defaultMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultMultiplier),
                defaultMultiplier,
                TypeResources.CostDirective_DefaultMultiplierCannotBeBelowTwo);
        }

        if (multipliers is null)
        {
            throw new ArgumentNullException(nameof(multipliers));
        }

        Complexity = complexity;
        DefaultMultiplier = defaultMultiplier;
        Multipliers = multipliers.Where(t => t.HasValue).ToArray();
    }

    private CostDirective(
        int complexity,
        IReadOnlyList<MultiplierPathString> multipliers,
        int? defaultMultiplier)
    {
        Complexity = complexity;
        Multipliers = multipliers;
        DefaultMultiplier = defaultMultiplier;
    }

    public int Complexity { get; }

    public IReadOnlyList<MultiplierPathString> Multipliers { get; }

    public int? DefaultMultiplier { get; }
}
