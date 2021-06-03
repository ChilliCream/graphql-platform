using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    [Obsolete("Use HotChocolate.Data.")]
    public sealed class UseSortingAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _generic = typeof(SortObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name.Equals(
                nameof(SortObjectFieldDescriptorExtensions.UseSorting),
                StringComparison.Ordinal)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

        /// <summary>
        /// Gets or sets the sort type which specifies the sort object structure.
        /// </summary>
        /// <value>The sort type</value>
        public Type? SortType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (SortType is null)
            {
                descriptor.UseSorting();
            }
            else
            {
                _generic.MakeGenericMethod(SortType).Invoke(null, new[] { descriptor });
            }
        }
    }
}
