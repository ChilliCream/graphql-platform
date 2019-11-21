using System.Text;
using System;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using IOPath = System.IO.Path;
using HCError = HotChocolate.IError;

namespace StrawberryShake.Tools
{
    public static class CommandTools
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
                message.Append($"{IOPath.GetFullPath((string)error.Extensions["fileName"])}");
            }

            if (error.Locations is { } && error.Locations.Count > 0)
            {
                HotChocolate.Location location = error.Locations[0];
                message.Append($"({location.Line},{location.Column}): ");
            }
            message.Append($"error {error.Code ?? "GQL"}: {error.Message}");

            Console.WriteLine(message.ToString());
        }

        public static void AddHelp<T>(this CommandLineApplication app)
            where T : IHelpTextGenerator, new()
        {
            app.HelpOption("-h|--help");
            app.HelpTextGenerator = new T();
        }
    }
}
