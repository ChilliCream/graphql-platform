using System;

namespace HotChocolate.Utilities
{
    public static class NumberUtilities
    {
        public static int setArrayLength(int n)
        {
            int length = 1;
            for (int factor = 10; factor < n; factor = factor * 10)
            {
                length++;
            }
            return length;
        }

        public static int setArrayLength(long n)
        {
            int length = 1;
            for (long factor = 10; factor < n; factor = factor * 10)
            {
                length++;
            }
            return length;
        }

        public static int[] numberToArray(int num)
        {
            var result = new int[setArrayLength(num)];
            for (int i = result.Length - 1; i >= 0; i--)
            {
                result[i] = num % 10;
                num /= 10;
            }
            return result;
        }

        public static int[] numberToArray(long num)
        {
            var result = new int[setArrayLength(num)];
            for (long i = result.Length - 1; i >= 0; i--)
            {
                result[i] = (int)(num % 10);
                num /= 10;
            }
            return result;
        }
    }
}
