using System;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J.Language.Converters
{
    public class DatetimeConverter : ICypherConverter
    {
        public string ToQuery(object value)
        {
            var dt = (DateTime) value;
            return $"datetime(\"{dt.ToString()}\")";
        }

        public object ToValue(object neoValue, Type target)
        {
            var zonedDateTime = (ZonedDateTime) neoValue;
            return zonedDateTime.ToDateTimeOffset().UtcDateTime;
        }
    }
}
