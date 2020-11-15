namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    public static class ProjectionVisitorContextExtensions
    {
        public static void ReportError<T>(
            this IProjectionVisitorContext<T> context,
            IError error) =>
            context.Errors.Add(error);

        public static ProjectionScope<T> GetScope<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek();

        public static T GetInstance<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Peek();

        public static void PushInstance<T>(
            this IProjectionVisitorContext<T> context,
            T nextExpression) =>
            context.Scopes.Peek().Instance.Push(nextExpression);

        public static T PopInstance<T>(this IProjectionVisitorContext<T> context) =>
            context.Scopes.Peek().Instance.Pop();

        public static ProjectionScope<T> PopScope<T>(
            this IProjectionVisitorContext<T> context) =>
            context.Scopes.Pop();
    }
}
