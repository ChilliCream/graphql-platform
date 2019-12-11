using System;
using System.Linq;
using System.Reflection;

namespace HotChocolate.Types
{
    public sealed class UseFilteringAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _generic = typeof(FilterObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name.Equals(
                nameof(FilterObjectFieldDescriptorExtensions.UseFiltering),
                StringComparison.Ordinal)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

        /// <summary>
        /// Gets or sets the filter type which specifies the filter object structure.
        /// </summary>
        /// <value>The filter type</value>
        public Type FilterType { get; set; }

        public override void OnConfigure(IObjectFieldDescriptor descriptor)
        {
            if (FilterType is null)
            {
                descriptor.UseFiltering();
            }
            else
            {
                _generic.MakeGenericMethod(FilterType).Invoke(null, new[] { descriptor });
            }
        }
    }
}