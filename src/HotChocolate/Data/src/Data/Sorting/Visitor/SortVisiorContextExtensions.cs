using System.Collections.Generic;

namespace HotChocolate.Data.Sorting
{
    public static class SortVisitorContextExtensions
    {
        public static void ReportError<T>
            (this ISortVisitorContext<T> context,
            IError error) =>
            context.Errors.Add(error);

        public static T GetInstance<T>(
            this ISortVisitorContext<T> context) =>
            context.Instance.Peek();

        public static void PushInstance<T>(
            this ISortVisitorContext<T> context, T nextExpression) =>
            context.Instance.Push(nextExpression);

        public static T PopInstance<T>(this ISortVisitorContext<T> context) =>
            context.Instance.Pop();
    }
}
