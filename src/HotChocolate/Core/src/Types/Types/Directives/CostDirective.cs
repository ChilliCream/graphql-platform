using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    [Serializable]
    public sealed class CostDirective
        : ISerializable
    {
        [NonSerialized]
        private readonly int _complexity;

        [NonSerialized]
        private readonly IReadOnlyList<MultiplierPathString> _multipliers;

        [NonSerialized]
        private readonly int? _defaultMultiplier;

        public CostDirective()
        {
            _complexity = 1;
            _multipliers = Array.Empty<MultiplierPathString>();
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

            _complexity = complexity;
            _multipliers = Array.Empty<MultiplierPathString>();
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

            _complexity = complexity;
            _multipliers = multipliers.Where(t => t.HasValue).ToArray();
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

            _complexity = complexity;
            _defaultMultiplier = defaultMultiplier;
            _multipliers = multipliers.Where(t => t.HasValue).ToArray();
        }

        private CostDirective(
            SerializationInfo info,
            StreamingContext context)
        {
            var node = info.GetValue(
                nameof(DirectiveNode),
                typeof(DirectiveNode))
                as DirectiveNode;

            if (node is null)
            {
                _complexity = info.GetInt32(nameof(Complexity));
                _defaultMultiplier = info.GetInt32(nameof(DefaultMultiplier));
                _multipliers = ((string[])info
                    .GetValue(nameof(Multipliers), typeof(string[])))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => new MultiplierPathString(s))
                    .ToArray();
            }
            else
            {
                ArgumentNode complexityArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "complexity");
                ArgumentNode multipliersArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "multipliers");
                ArgumentNode defaultMultiplierArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "defaultMultiplier");

                _complexity = complexityArgument is { Value: IntValueNode iv }
                    ? int.Parse(iv.Value)
                    : 1;

                _multipliers = multipliersArgument switch
                {
                    { Value: ListValueNode lv } =>
                        lv.Items.OfType<StringValueNode>()
                            .Select(t => t.Value.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => new MultiplierPathString(s))
                            .ToArray(),
                    { Value: StringValueNode sv } =>
                        new[] { new MultiplierPathString(sv.Value.Trim()) },
                    _ => Array.Empty<MultiplierPathString>()
                };

                _defaultMultiplier = (defaultMultiplierArgument?.Value as IntValueNode)?.ToInt32();
            }
        }

        public int Complexity => _complexity;

        public IReadOnlyList<MultiplierPathString> Multipliers => _multipliers;

        public int? DefaultMultiplier => _defaultMultiplier;

        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            info.AddValue(nameof(Complexity), Complexity);
            info.AddValue(nameof(Multipliers), Multipliers);
            info.AddValue(nameof(DefaultMultiplier), DefaultMultiplier);
        }
    }
}
