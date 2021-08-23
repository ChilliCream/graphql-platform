using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake.Helper
{
    public static class ComparisonHelper
    {
        public static bool SequenceEqual<TSource>(
            IEnumerable<TSource>? first,
            IEnumerable<TSource>? second)
        {
            if (first is null && second is null)
            {
                return true;
            }

            if (second is not null && first is not null)
            {
                return first.SequenceEqual(second);
            }

            return false;
        }

        public static bool SequenceEqual<TSource>(
            IEnumerable<IEnumerable<TSource>?>? first,
            IEnumerable<IEnumerable<TSource>?>? second)
        {
            if (first is null && second is null)
            {
                return true;
            }

            if (first is not null && second is not null)
            {
                using IEnumerator<IEnumerable<TSource>?> e1 = first.GetEnumerator();
                using IEnumerator<IEnumerable<TSource>?> e2 = second.GetEnumerator();

                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && SequenceEqual(e1.Current, e2.Current)))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }

            return false;
        }

        public static bool SequenceEqual(
            IEnumerable? first,
            IEnumerable? second)
        {
            if (first is null && second is null)
            {
                return true;
            }

            if (first is not null && second is not null)
            {
                IEnumerator e1 = first.GetEnumerator();
                IEnumerator e2 = second.GetEnumerator();

                while (e1.MoveNext())
                {
                    if (!e2.MoveNext())
                    {
                        return false;
                    }

                    if (e1.Current is null && e2.Current is not null)
                    {
                        return false;
                    }

                    if (e1.Current is not null && e2.Current is null)
                    {
                        return false;
                    }

                    if (e1.Current is IEnumerable i1 &&
                        e2.Current is IEnumerable i2 &&
                        !SequenceEqual(i1, i2))
                    {
                        return false;
                    }

                    if (e1.Current is not null &&
                        e2.Current is not null &&
                        !e1.Current.Equals(e2.Current))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }

            return false;
        }
    }
}
