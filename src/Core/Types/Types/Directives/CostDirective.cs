using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class CostDirective
        : ISerializable
    {
        public CostDirective()
        {
            Complexity = 1;
            Multipliers = Array.Empty<string>();
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

            Complexity = complexity;
            Multipliers = Array.Empty<string>();
        }

        public CostDirective(int complexity, params string[] multipliers)
        {
            if (complexity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(complexity),
                    complexity,
                    "The complexity cannot be below one.");
            }

            if (multipliers == null)
            {
                throw new ArgumentNullException(nameof(multipliers));
            }

            Complexity = complexity;
            Multipliers = multipliers
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();
        }

        protected CostDirective(
            SerializationInfo info,
            StreamingContext context)
        {
            var node = info.GetValue(
                nameof(DirectiveNode),
                typeof(DirectiveNode))
                as DirectiveNode;

            if (node == null)
            {
                Complexity = info.GetInt32(nameof(Complexity));
                Multipliers = (IReadOnlyCollection<string>)info
                    .GetValue(nameof(Multipliers), typeof(string[]));
            }
            else
            {
                ArgumentNode complexityArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "complexity");
                ArgumentNode multipliersArgument = node.Arguments
                    .FirstOrDefault(t => t.Name.Value == "multipliers");

                Complexity = (complexityArgument != null
                    && complexityArgument.Value is IntValueNode iv)
                    ? int.Parse(iv.Value)
                    : 1;

                Multipliers = (multipliersArgument != null
                    && multipliersArgument.Value is ListValueNode lv)
                    ? lv.Items.OfType<StringValueNode>()
                        .Select(t => t.Value).ToArray()
                    : Array.Empty<string>();
            }
        }

        public int Complexity { get; }

        public IReadOnlyCollection<string> Multipliers { get; }

        public void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            info.AddValue(nameof(Complexity), Complexity);
            info.AddValue(nameof(Multipliers), Multipliers.ToArray());
        }
    }
}
