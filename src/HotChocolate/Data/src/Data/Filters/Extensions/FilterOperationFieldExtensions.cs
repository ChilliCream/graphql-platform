namespace HotChocolate.Data.Filters;

/// <summary>
/// Common extension for allowing specifc filter operations on fields
/// </summary>
public static class FilterOperationFieldExtensions
{
    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Equals"/> is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Equals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotEquals"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEquals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Contains"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowContains(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Contains);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotContains"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotContains(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotContains);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.In"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowIn(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.In);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotIn"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotIn(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotIn);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.StartsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowStartsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.StartsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotStartsWith"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotStartsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotStartsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.EndsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowEndsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.EndsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotEndsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotEndsWith(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEndsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.And"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowAnd(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.And);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Or"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowOr(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Or);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.GreaterThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowGreaterThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotGreaterThan"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotGreaterThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.GreaterThanOrEquals"/> operation is allow
    /// on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowGreaterThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThanOrEquals);
        return descriptor;
    }
    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotGreaterThanOrEquals"/> operation is
    /// allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotGreaterThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThanOrEquals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.LowerThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowLowerThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotLowerThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotLowerThan(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.LowerThanOrEquals"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowLowerThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThanOrEquals);
        return descriptor;
    }
    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotLowerThanOrEquals"/> operation is allow
    /// on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterFieldDescriptor AllowNotLowerThanOrEquals(this IFilterFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThanOrEquals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Equals"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Equals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotEquals"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEquals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Contains"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowContains(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Contains);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotContains"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotContains(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotContains);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.In"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowIn(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.In);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotIn"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotIn(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotIn);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.StartsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowStartsWith(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.StartsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotStartsWith"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotStartsWith(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotStartsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.EndsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowEndsWith(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.EndsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotEndsWith"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotEndsWith(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotEndsWith);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.And"/> operation is allow on this field
    ///
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowAnd(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.And);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.Or"/> operation is allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowOr(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.Or);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.GreaterThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowGreaterThan(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotGreaterThan"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotGreaterThan(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.GreaterThanOrEquals"/> operation is allow
    /// on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowGreaterThanOrEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.GreaterThanOrEquals);
        return descriptor;
    }
    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotGreaterThanOrEquals"/> operation is
    /// allow on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotGreaterThanOrEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotGreaterThanOrEquals);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.LowerThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowLowerThan(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotLowerThan"/> operation is allow on this
    /// field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotLowerThan(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThan);
        return descriptor;
    }

    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.LowerThanOrEquals"/> operation is allow on
    /// this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowLowerThanOrEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.LowerThanOrEquals);
        return descriptor;
    }
    /// <summary>
    /// Defines if the <see cref="DefaultFilterOperations.NotLowerThanOrEquals"/> operation is allow
    /// on this field
    /// </summary>
    /// <param name="descriptor">
    /// The descriptor where the operation should be allowed
    /// </param>
    public static IFilterOperationFieldDescriptor AllowNotLowerThanOrEquals(
        this IFilterOperationFieldDescriptor descriptor)
    {
        descriptor.AllowOperation(DefaultFilterOperations.NotLowerThanOrEquals);
        return descriptor;
    }
}
