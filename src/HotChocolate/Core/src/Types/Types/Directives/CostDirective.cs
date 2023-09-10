using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// The cost directive can be used to express the expected
/// cost that a resolver incurs on the system.
/// </summary>
#if NET6_0_OR_GREATER
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
public sealed class CostDirective
{
    /// <summary>
    /// Initializes a new instance of <see cref="CostDirective"/>.
    /// </summary>
    public CostDirective()
    {
        Complexity = 1;
        Multipliers = Array.Empty<MultiplierPathString>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CostDirective"/>.
    /// </summary>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="complexity"/> is less than 0.
    /// </exception>
    public CostDirective(int complexity)
    {
        if (complexity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(complexity),
                complexity,
                TypeResources.CostDirective_ComplexityCannotBeBelowOne);
        }

        Complexity = complexity;
        Multipliers = Array.Empty<MultiplierPathString>();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CostDirective"/>.
    /// </summary>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <param name="multipliers">
    /// The multiplier paths.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="complexity"/> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="multipliers"/> is <c>null</c>.
    /// </exception>
    public CostDirective(
        int complexity,
        params MultiplierPathString[] multipliers)
    {
        if (complexity < 0)
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

    /// <summary>
    /// Initializes a new instance of <see cref="CostDirective"/>.
    /// </summary>
    /// <param name="complexity">
    /// The complexity of the field.
    /// </param>
    /// <param name="defaultMultiplier">
    /// The default multiplier.
    /// </param>
    /// <param name="multipliers">
    /// The multiplier paths.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="complexity"/> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="multipliers"/> is <c>null</c>.
    /// </exception>
    public CostDirective(
        int complexity,
        int defaultMultiplier,
        params MultiplierPathString[] multipliers)
    {
        if (complexity < 0)
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

    // this constructor is used for serialization.
    private CostDirective(
        int complexity,
        IReadOnlyList<MultiplierPathString> multipliers,
        int? defaultMultiplier)
    {
        Complexity = complexity;
        Multipliers = multipliers;
        DefaultMultiplier = defaultMultiplier;
    }

    /// <summary>
    /// Gets the complexity of the field.
    /// </summary>
    public int Complexity { get; }

    /// <summary>
    /// Gets the multiplier paths.
    /// </summary>
    public IReadOnlyList<MultiplierPathString> Multipliers { get; }

    /// <summary>
    /// Gets the default multiplier.
    /// </summary>
    public int? DefaultMultiplier { get; }
}
