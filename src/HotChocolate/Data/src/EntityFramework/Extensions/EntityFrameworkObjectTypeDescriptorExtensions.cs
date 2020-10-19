using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Data;
using HotChocolate.Data.DataLoaders;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Types
{
    public static class EntityFrameworkObjectTypeDescriptorExtensions
    {
        public static IObjectFieldDescriptor Entity<TKey, TData, TProp, TDb>(
            this IObjectTypeDescriptor<TData> desc, Expression<Func<TData, ICollection<TProp>>> propertyOrMethod
        ) where TData : IModelId<TKey> where TProp : class, IModelId<TKey> where TKey : class where TDb : DbContext =>
            desc.Field(propertyOrMethod)
                .ResolveWith<EntityFrameworkResolver<TKey, TData, TProp, TDb>>(r =>
                     r.ManyToMany(default!, default!, default!));
    }
}
