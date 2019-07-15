using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal static class QueryResultHelper
    {
        private const string _data = "data";
        private const string _errors = "errors";
        private const string _extensions = "extensions";
        private const string _message = "message";
        private const string _locations = "locations";
        private const string _path = "path";

        public static IReadOnlyDictionary<string, object> ToDictionary(
            IReadOnlyQueryResult result)
        {
            var formatted = new OrderedDictionary();

            if (result.Errors.Count > 0)
            {
                formatted[_errors] = SerializeErrors(result.Errors);
            }

            if (result.Data.Count > 0)
            {
                formatted[_data] = result.Data;
            }

            if (result.Extensions.Count > 0)
            {
                formatted[_extensions] = result.Extensions;
            }

            return formatted;
        }

        private static ICollection<object> SerializeErrors(
            IReadOnlyCollection<IError> errors)
        {
            var formattedErrors = new List<object>();

            foreach (IError error in errors)
            {
                var formattedError = new OrderedDictionary();
                formattedError[_message] = error.Message;

                if (error.Locations != null && error.Locations.Count > 0)
                {
                    formattedError[_locations] = error.Locations;
                }

                if (error.Path != null && error.Path.Count > 0)
                {
                    formattedError[_path] = error.Path;
                }

                if (error.Extensions != null && error.Extensions.Count > 0)
                {
                    formattedError[_extensions] = error.Extensions;
                }

                formattedErrors.Add(formattedError);
            }

            return formattedErrors;
        }
    }
}
