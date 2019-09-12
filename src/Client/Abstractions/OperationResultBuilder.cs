namespace StrawberryShake
{
    public static class OperationResultBuilder
    {
        public static OperationResultBuilder<T> New<T>()
            where T : class =>
            new OperationResultBuilder<T>();

        public static OperationResultBuilder<T> FromResult<T>(
            IOperationResult<T> result)
            where T : class =>
            new OperationResultBuilder<T>(result);
    }
}
