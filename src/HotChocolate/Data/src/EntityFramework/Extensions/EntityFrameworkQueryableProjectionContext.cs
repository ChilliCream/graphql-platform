using System;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Extensions;
internal class EntityFrameworkQueryableProjectionContext : QueryableProjectionContext
{
    public EntityFrameworkQueryableProjectionContext(IResolverContext context,
        IOutputType initialType,
        Type runtimeType,
        bool useKeysForNullCheck) : base(context, initialType, runtimeType)
    {
        UseKeysForNullCheck = useKeysForNullCheck;
    }

    public bool UseKeysForNullCheck { get; }
}
