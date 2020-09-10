using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Configuration
{
    /// <summary>
    /// The type discovery context is available during the discovery phase of the type system.
    /// In this phase types are inspected and registered.
    /// </summary>
    public interface ITypeDiscoveryContext
        : ITypeSystemObjectContext
    {
        /// <summary>
        /// The collected type dependencies.
        /// </summary>
        IReadOnlyList<TypeDependency> TypeDependencies { get; }

        /// <summary>
        /// Register a reference to a type on which <see cref="Type" /> depends. 
        /// Such a reference could for instance represent a type of a field that 
        /// <see cref="Type" /> exposes.
        /// </summary>
        /// <param name="reference">
        /// A reference representing a type on which <see cref="Type" /> depends.
        /// </param>
        /// <param name="kind">
        /// The type dependency context defines if this type for instance is 
        /// discovered in an input context.
        /// </param>
        void RegisterDependency(
            ITypeReference reference,
            TypeDependencyKind kind);

        /// <summary>
        /// Register a reference to a type on which <see cref="Type" /> depends. 
        /// Such a reference could for instance represent a type of a field that 
        /// <see cref="Type" /> exposes.
        /// </summary>
        /// <param name="dependency">
        /// A type dependency containing the type reference and context representing 
        /// a type on which <see cref="Type" /> depends.
        /// </param>
        void RegisterDependency(
            TypeDependency dependency);

        /// <summary>
        /// Register multiple references to types on which <see cref="Type" /> depends. 
        /// Such a reference could for instance represent a type of a field that 
        /// <see cref="Type" /> exposes.
        /// </summary>
        /// <param name="references">
        /// Type references representing types on which <see cref="Type" /> depends.
        /// </param>
        /// <param name="kind">
        /// The type dependency context defines if this type for instance is 
        /// discovered in an input context.
        /// </param>
        void RegisterDependencyRange(
            IEnumerable<ITypeReference> references,
            TypeDependencyKind kind);

        /// <summary>
        /// Register multiple references to types on which <see cref="Type" /> depends.
        /// Such a reference could for instance represent a type of a field that 
        /// <see cref="Type" /> exposes.
        /// </summary>
        /// <param name="dependencies">
        /// Type dependencies containing the type reference and context representing 
        /// a types on which <see cref="Type" /> depends.
        /// </param>
        void RegisterDependencyRange(
            IEnumerable<TypeDependency> dependencies);

        /// <summary>
        /// Registers a reference to a directive on which <see cref="Type" /> depends.
        /// </summary>
        /// <param name="reference">
        /// A reference to a directive.
        /// </param>
        void RegisterDependency(
            IDirectiveReference reference);

        /// <summary>
        /// Registers multiple references to directives on which <see cref="Type" /> depends.
        /// </summary>
        /// <param name="references">
        /// Multiple references to a directives.
        /// </param>
        void RegisterDependencyRange(
            IEnumerable<IDirectiveReference> references);

        /// <summary>
        /// Registers a resolver for compilation.
        /// </summary>
        /// <param name="fieldName">The field name to which the resolver belongs to.</param>
        /// <param name="member">The member that represents the resolver.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="resolverType">The type that declares the resolver.</param>
        void RegisterResolver(
            NameString fieldName,
            MemberInfo member,
            Type sourceType,
            Type resolverType);

        /// <summary>
        /// Registers a resolver for compilation.
        /// </summary>
        /// <param name="fieldName">The field name to which the resolver belongs to.</param>
        /// <param name="expression">The expression representing the resolver.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="resolverType">The type that declares the resolver.</param>
        void RegisterResolver(
            NameString fieldName,
            Expression expression,
            Type sourceType,
            Type resolverType);
    }
}
