using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;

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
                    "The complexity cannot be below one.");
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
                    "The complexity cannot be below one.");
            }

            if (multipliers is null)
            {
                throw new ArgumentNullException(nameof(multipliers));
            }

            _complexity = complexity;
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

                _complexity = (complexityArgument != null
                    && complexityArgument.Value is IntValueNode iv)
                    ? int.Parse(iv.Value)
                    : 1;

                _multipliers = (multipliersArgument != null
                    && multipliersArgument.Value is ListValueNode lv)
                    ? lv.Items.OfType<StringValueNode>()
                        .Select(t => t.Value?.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => new MultiplierPathString(s))
                        .ToArray()
                    : Array.Empty<MultiplierPathString>();
            }
        }

        public int Complexity => _complexity;

        public IReadOnlyList<MultiplierPathString> Multipliers => _multipliers;

        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            info.AddValue(nameof(Complexity), Complexity);
            info.AddValue(nameof(Multipliers), Multipliers.ToArray());
        }
    }
}
