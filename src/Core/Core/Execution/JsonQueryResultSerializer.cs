using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        private const string _data = "data";
        private const string _errors = "errors";
        private const string _extensions = "extensions";
        private const string _message = "message";
        private const string _locations = "locations";
        private const string _path = "path";


        public async Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream)
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

            byte[] buffer = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(formatted));

            await stream.WriteAsync(buffer, 0, buffer.Length)
                .ConfigureAwait(false);
        }

        private ICollection<object> SerializeErrors(
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
