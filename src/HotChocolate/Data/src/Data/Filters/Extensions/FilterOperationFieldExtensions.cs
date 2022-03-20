namespace HotChocolate.Data.Filters;

public static class FilterOperationFieldExtensions
{
    public static IFilterFieldDescriptor AllowEqual(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Equals);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Equals);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEquals);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowContains(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Contains);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotContains(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotContains);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowIn(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.In);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotIn(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotIn);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowStartsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.StartsWith);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotStartsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotStartsWith);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowEndsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.EndsWith);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotEndsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEndsWith);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowAnd(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.And);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowOr(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Or);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowGreaterThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThan);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotGreaterThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThan);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowGreaterThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThanOrEquals);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotGreaterThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThanOrEquals);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowLowerThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThan);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotLowerThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThan);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowLowerThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThanOrEquals);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNotLowerThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThanOrEquals);
        return descriptor;
    }

    public static IFilterFieldDescriptor AllowSome(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Some);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowAll(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.All);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowNone(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.None);
        return descriptor;
    }
    public static IFilterFieldDescriptor AllowAny(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Any);
        return descriptor;
    }
}
