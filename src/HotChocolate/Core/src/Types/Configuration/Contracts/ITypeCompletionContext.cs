using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

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

        IsOfTypeFallback IsOfType { get; }

        bool TryGetType<T>(
            ITypeReference typeRef,
            [NotNullWhen(true)]out T type)
            where T : IType;

        T GetType<T>(ITypeReference typeRef) where T : IType;

        IEnumerable<T> GetTypes<T>() where T : IType;

        DirectiveType GetDirectiveType(IDirectiveReference directiveRef);

        FieldResolver GetResolver(NameString fieldName);

        Func<ISchema> GetSchemaResolver();
    }
}
