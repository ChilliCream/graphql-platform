using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Configuration
{
    public interface ITypeCompletionContext
        : ITypeSystemObjectContext
    {
        /// <summary>
        /// Defines if the type that is being completed is the query type.
        /// </summary>
        bool? IsQueryType { get; }

        IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        IsOfTypeFallback? IsOfType { get; }

        /// <summary>
        /// Tries to resolve a type by its <paramref name="typeRef" />.
        /// </summary>
        /// <typeparam name="T">
        /// The expected type.
        /// </typeparam>
        /// <param name="typeRef">
        /// The type reference representing the type.
        /// </param>
        /// <param name="type">
        /// The resolved types.
        /// </param>
        /// <returns>
        /// <c>true</c> if the type has been resolved; otherwise, <c>false</c>.
        /// </returns>
        bool TryGetType<T>(ITypeReference typeRef, [NotNullWhen(true)] out T type) where T : IType;

        /// <summary>
        /// Gets a type by it's type reference.
        /// </summary>
        /// <param name="typeRef">
        /// The type reference representing the type.
        /// </param>
        /// <typeparam name="T">
        /// The expected type.
        /// </typeparam>
        /// <returns>
        /// The resolved types.
        /// </returns>
        /// <exception cref="SchemaException">
        /// The type could not be resolved for the given <paramref name="typeRef" />.
        /// </exception>
        T GetType<T>(ITypeReference typeRef) where T : IType;

        IEnumerable<T> GetTypes<T>() where T : IType;

        bool TryGetDirectiveType(
            IDirectiveReference directiveRef,
            [NotNullWhen(true)] out DirectiveType? directiveType);

        DirectiveType GetDirectiveType(IDirectiveReference directiveRef);

        FieldResolver GetResolver(NameString fieldName);

        Func<ISchema> GetSchemaResolver();
    }
}
