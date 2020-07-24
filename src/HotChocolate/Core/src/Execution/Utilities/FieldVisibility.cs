using System;
using System.Diagnostics;
using HotChocolate.Language;
using static HotChocolate.Execution.Utilities.ThrowHelper;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class FieldVisibility
    {
        internal FieldVisibility(
            IValueNode? skip = null,
            IValueNode? include = null,
            FieldVisibility? parent = null)
        {
            Debug.Assert(
                skip != null || include != null,
                "Either skip or include should be set, otherwise the instance would be wasted.");

            Skip = skip;
            Include = include;
            Parent = parent;
        }

        public IValueNode? Skip { get; }

        public IValueNode? Include { get; }

        public FieldVisibility? Parent { get; }

        public bool IsVisible(IVariableValueCollection variables)
        {
            if (Parent != null && !Parent.IsVisible(variables))
            {
                return false;
            }

            if (Skip != null && IsTrue(variables, Skip))
            {
                return false;
            }

            return Include is null || IsTrue(variables, Include);
        }

        private static bool IsTrue(
            IVariableValueCollection variables,
            IValueNode value)
        {
            if (value is BooleanValueNode b)
            {
                return b.Value;
            }

            if (value is VariableNode v)
            {
                return variables.GetVariable<bool>(v.Name.Value);
            }

            throw FieldVisibility_ValueNotSupported(value);
        }

        public bool Equals(IValueNode? skip, IValueNode? include)
        {
            return EqualsInternal(skip, Skip) && EqualsInternal(include, Include);
        }

        public bool Equals(FieldVisibility visibility)
        {
            if (Equals(visibility.Skip, visibility.Include))
            {
                if (Parent is null)
                {
                    return visibility.Parent is null;
                }
                else
                {
                    return visibility.Parent is { } ? Parent.Equals(visibility.Parent) : false;
                }
            }
            return false;
        }

        private static bool EqualsInternal(IValueNode? a, IValueNode? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is BooleanValueNode ab &&
                b is BooleanValueNode bb &&
                ab.Value == bb.Value)
            {
                return true;
            }

            if (a is VariableNode av &&
                b is VariableNode bv &&
                string.Equals(av.Value, bv.Value, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
