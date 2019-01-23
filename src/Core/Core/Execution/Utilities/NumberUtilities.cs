using System;

namespace HotChocolate.Execution
{
    internal static class NumberUtilities
    {
        internal static int GetDigitCount(int n)
        {
            if (n >= 100000)
            {
                if (n >= 1000000000)
                {
                    return 10;
                }
                if (n >= 100000000)
                {
                    return 9;
                }
                if (n >= 10000000)
                {
                    return 8;
                }
                if (n >= 1000000)
                {
                    return 7;
                }
                if (n >= 100000)
                {
                    return 6;
                }
            }
            else
            {
                if (n >= 10000)
                {
                    return 5;
                }
                if (n >= 1000)
                {
                    return 4;
                }
                if (n >= 100)
                {
                    return 3;
                }
                if (n >= 10)
                {
                    return 2;
                }
                if (n >= 0)
                {
                    return 1;
                }
            }

            return 0;
        }

        internal static int GetDigitCount(ulong n)
        { 
            if (n <= int.MaxValue)
            {
                return GetDigitCount((int)n);
            }

            if (n > int.MaxValue && n < 10000000000)
            {
                return 10;
            }

            // Max length for ulong is 20
            if (n >= 1000000000000000)
            {
                if (n >= 10000000000000000000)
                {
                    return 20;
                }
                if (n >= 1000000000000000000)
                {
                    return 19;
                }
                if (n >= 100000000000000000)
                {
                    return 18;
                }
                if (n >= 10000000000000000)
                {
                    return 17;
                }
                if (n >= 1000000000000000)
                {
                    return 16;
                }
            }
            else
            {
                if (n >= 100000000000000)
                {
                    return 15;
                }
                if (n >= 10000000000000)
                {
                    return 14;
                }
                if (n >= 1000000000000)
                {
                    return 13;
                }
                if (n >= 100000000000)
                {
                    return 12;
                }
            }

            return 11;
        }

        internal static int[] NumberToArray(ulong num)
        {
            var result = new int[GetDigitCount(num)];
            for (var i = result.Length - 1; i >= 0; i--)
            {
                result[i] = (int)(num % 10);
                num /= 10;
            }
            return result;
        }
    }
}
