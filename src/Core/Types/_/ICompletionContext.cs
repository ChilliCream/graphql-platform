using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate
{
    public interface ICompletionContext
        : ITypeSystemObjectContext
    {
        bool? IsQueryType { get; }

        IReadOnlyList<FieldMiddleware> GlobalComponents { get; }

        bool TryGetType<T>(ITypeReference reference, out T type) where T : IType;

        T GetType<T>(ITypeReference reference) where T : IType;

        IEnumerable<IType> GetTypes();

        DirectiveType GetDirectiveType(IDirectiveReference reference);

        FieldResolver GetResolver(IFieldReference reference);

        IReadOnlyCollection<ObjectType> GetPossibleTypes();

        Func<ISchema> GetSchemaResolver();
    }
}
