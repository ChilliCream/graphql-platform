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
        bool TryGetType<T>(ITypeReference reference, out T type) where T : IType;

        T GetType<T>(ITypeReference reference) where T : IType;

        DirectiveType GetDirectiveType(IDirectiveReference reference);

        FieldResolver GetResolver(IFieldReference reference);

        FieldDelegate GetCompiledMiddleware(IFieldReference reference);

        IReadOnlyCollection<ObjectType> GetPossibleTypes();

        Func<ISchema> GetSchemaResolver();
    }
}
