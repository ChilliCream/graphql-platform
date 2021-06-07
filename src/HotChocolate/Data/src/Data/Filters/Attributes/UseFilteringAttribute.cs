using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    /// <summary>
    /// Registers the middleware and adds the arguments for filtering
    /// </summary>
    public sealed class UseFilteringAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _generic = typeof(FilterObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(
                m => m.Name.Equals(
                        nameof(FilterObjectFieldDescriptorExtensions.UseFiltering),
                        StringComparison.Ordinal) &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

        public UseFilteringAttribute(Type? filterType = null)
        {
            Type = filterType;
        }

        /// <summary>
        /// Gets or sets the filter type which specifies the filter object structure.
        /// </summary>
        /// <value>The filter type</value>
        public Type? Type { get; set; }

        /// <summary>
        /// Sets the scope for the convention
        /// </summary>
        /// <value>The name of the scope</value>
        public string? Scope { get; set; }

        /// <inheritdoc />
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (Type is null)
            {
                descriptor.UseFiltering(Scope);
            }
            else
            {
                _generic.MakeGenericMethod(Type)
                    .Invoke(
                        null,
                        new object?[] { descriptor, Scope });
            }
        }
    }
}
