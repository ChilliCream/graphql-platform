namespace StrawberryShake
{
    public static class OperationResultBuilder
    {
        public static OperationResultBuilder<T> New<T>() =>
            new OperationResultBuilder<T>();
    }
}
