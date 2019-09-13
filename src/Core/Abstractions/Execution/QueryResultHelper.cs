using System;
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
        private const string _line = "line";
        private const string _column = "column";

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
                    formattedError[_locations] = SerializeLocations(error.Locations);
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

        private static IReadOnlyList<IReadOnlyDictionary<string, int>> SerializeLocations(
            IReadOnlyList<Location> locations)
        {
            var serializedLocations = new IReadOnlyDictionary<string, int>[locations.Count];

            for (int i = 0; i < locations.Count; i++)
            {
                Location location = locations[i];
                serializedLocations[i] = new OrderedDictionary<string, int>
                {
                    { _line, location.Line },
                    { _column, location.Column }
                };
            }

            return serializedLocations;
        }
    }
}
