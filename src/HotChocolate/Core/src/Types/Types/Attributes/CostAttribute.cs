using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The cost directive can be used to express the expected
/// cost that a resolver incurs on the system.
/// </summary>
public sealed class CostAttribute : ObjectFieldDescriptorAttribute
{
    private readonly int _complexity;
    private readonly int? _defaultMultiplier;
    private readonly MultiplierPathString[] _multipliers;

    public CostAttribute()
    {
        _complexity = 1;
        _multipliers = Array.Empty<MultiplierPathString>();
    }

    public CostAttribute(int complexity)
    {
        if (complexity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(complexity),
                complexity,
                TypeResources.CostDirective_ComplexityCannotBeBelowOne);
        }

        _complexity = complexity;
        _multipliers = Array.Empty<MultiplierPathString>();
    }

    public CostAttribute(
        int complexity,
        params string[] multipliers)
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

        _complexity = complexity;
        _multipliers = multipliers.Select(t => new MultiplierPathString(t)).Where(t => t.HasValue).ToArray();
    }

    public CostAttribute(
        int complexity,
        int defaultMultiplier,
        params string[] multipliers)
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

        _complexity = complexity;
        _defaultMultiplier = defaultMultiplier;
        _multipliers = multipliers.Select(t => new MultiplierPathString(t)).Where(t => t.HasValue).ToArray();
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        if (_defaultMultiplier.HasValue)
        {
            descriptor.Directive(
                new CostDirective(
                    _complexity,
                    _defaultMultiplier.Value,
                    _multipliers));
        }
        else
        {
            descriptor.Directive(
                new CostDirective(
                    _complexity,
                    _multipliers));
        }
    }
}
