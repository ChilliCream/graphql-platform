// Polyfills for netstandard2.0 to enable modern C# features that the compiler
// expects specific types to exist for (init-only setters, required members, etc.).

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Represents a polyfill that enables init-only property setters on netstandard2.0.
    /// </summary>
    internal sealed class IsExternalInit;

    /// <summary>
    /// Represents a polyfill that enables the <see langword="required"/> modifier on netstandard2.0.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
        AllowMultiple = false,
        Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute;

    /// <summary>
    /// Represents a polyfill that enables compiler feature gating on netstandard2.0.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerFeatureRequiredAttribute"/> class
        /// with the specified feature name.
        /// </summary>
        /// <param name="featureName">The name of the compiler feature that is required.</param>
        public CompilerFeatureRequiredAttribute(string featureName) => FeatureName = featureName;

        /// <summary>
        /// Gets the name of the compiler feature that is required.
        /// </summary>
        public string FeatureName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the feature requirement is optional.
        /// </summary>
        public bool IsOptional { get; init; }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Represents a polyfill that enables the <see langword="required"/> modifier on constructor
    /// parameters for netstandard2.0.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute;
}
