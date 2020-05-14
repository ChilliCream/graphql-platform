using System.Collections.Generic;
using System.IO;
using System.Text;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools.Extensions
{
    public static class ErrorExtensions
    {
        public static void Write(this IEnumerable<HCError> errors)
        {
            var message = new StringBuilder();

            foreach (HCError error in errors)
            {
                Write(error, message);
            }
        }

        public static void Write(this HCError error)
        {
            Write(error, new StringBuilder());
        }

        public static void Write(this HCError error, StringBuilder message)
        {
            message.Clear();

            if (error.Extensions is { } && error.Extensions.ContainsKey("fileName"))
            {
                message.Append($"{Path.GetFullPath((string)error.Extensions["fileName"])}");
            }

            if (error.Locations is { } && error.Locations.Count > 0)
            {
                HotChocolate.Location location = error.Locations[0];
                message.Append($"({location.Line},{location.Column}): ");
            }
            message.Append($"error {error.Code ?? "GQL"}: {error.Message}");

            System.Console.Error.WriteLine(message.ToString());
        }
    }
}
