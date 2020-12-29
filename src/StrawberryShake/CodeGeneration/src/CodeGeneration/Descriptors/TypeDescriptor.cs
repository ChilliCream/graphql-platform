using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type, which may be a concrete class or an interface.
    /// </summary>
    public class TypeDescriptor
        : ICodeDescriptor
    {
        /// <summary>
        /// Gets the .NET type name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The properties that result from the requested fields of the operation this ResultType is generated for.
        /// </summary>
        public IReadOnlyList<TypePropertyDescriptor> Properties { get; }

        /// <summary>
        /// A list of interface names the ResultType implements
        /// </summary>
        public IReadOnlyList<string> Implements { get; }

        /// <summary>
        /// The name of the namespace the generated type shall reside in
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// States whether or not this type is an interface
        /// </summary>
        public bool IsInterface => IsImplementedBy.Count > 0;

        /// <summary>
        /// A list of types that implement this interface
        /// This list must only contain the most specific, concrete classes (that implement this interface),
        /// but no other interfaces.
        /// </summary>
        public IReadOnlyList<string> IsImplementedBy { get; }

        public TypeDescriptor(
            string name,
            string @namespace,
            IReadOnlyList<string> implements,
            IReadOnlyList<TypePropertyDescriptor> properties)
        {
            Name = name;
            Properties = properties;
            Namespace = @namespace;
            Implements = implements;
        }
    }
}
