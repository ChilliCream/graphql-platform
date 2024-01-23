using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Properties.EntityFrameworkResources;

namespace HotChocolate.Data;

public class UseDbContextAttribute : ObjectFieldDescriptorAttribute
{
    private static readonly MethodInfo _useDbContext =
        typeof(EntityFrameworkObjectFieldDescriptorExtensions)
            .GetMethod(
                nameof(EntityFrameworkObjectFieldDescriptorExtensions.UseDbContext),
                BindingFlags.Public | BindingFlags.Static)!;

    private readonly Type _dbContext;

    public UseDbContextAttribute(Type dbContext, [CallerLineNumber] int order = 0)
    {
        _dbContext = dbContext;
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        if (!typeof(DbContext).IsAssignableFrom(_dbContext))
        {
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        UseDbContextAttribute_OnConfigure_NoDbContextType,
                        _dbContext.FullName ?? _dbContext.Name)
                    .SetExtension(nameof(member), member)
                    .Build());
        }

        _useDbContext.MakeGenericMethod(_dbContext).Invoke(null, [descriptor,]);
    }
}
