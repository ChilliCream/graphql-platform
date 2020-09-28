using System;

namespace GreenDonut
{
    public static class DataLoaderOptionsExtensions
    {
        public static int GetBatchSize<TKey>(this DataLoaderOptions<TKey> options)
            where TKey : notnull
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Batch)
            {
                if (options.MaxBatchSize < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(options.MaxBatchSize),
                        "The max batch size must be greater or equal 0.");
                }

                return options.MaxBatchSize == 0 ? int.MaxValue : options.MaxBatchSize;
            }

            return 1;
        }
    }
}
