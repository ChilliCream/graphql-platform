using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    internal static class PatternExtensions
    {
        public static bool TryParse<NodaTimeType>(
            this IPattern<NodaTimeType> pattern,
            string text,
            out NodaTimeType? output)
            where NodaTimeType : struct
        {
            ParseResult<NodaTimeType> result = pattern.Parse(text);

            if (result.Success)
            {
                output = result.Value;
                return true;
            }

            output = null;
            return false;
        }

        public static bool TryParse<NodaTimeType>(
            this IPattern<NodaTimeType> pattern,
            string text,
            out NodaTimeType? output)
            where NodaTimeType : class
        {
            ParseResult<NodaTimeType> result = pattern.Parse(text);

            if (result.Success)
            {
                output = result.Value;
                return true;
            }

            output = null;
            return false;
        }
    }
}
