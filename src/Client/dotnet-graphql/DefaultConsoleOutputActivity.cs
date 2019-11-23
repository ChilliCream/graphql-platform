using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StrawberryShake.Tools
{
    public class DefaultConsoleOutputActivity
        : IActivity
    {
        private readonly string _activityText;
        private readonly string? _path;
        private readonly Stopwatch _stopwatch;
        private bool _hasErrors;

        public DefaultConsoleOutputActivity(string activityText, string? path)
        {
            _activityText = activityText;
            _path = path;
            _stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"{activityText} started.");
        }

        public void WriteError(HotChocolate.IError error)
        {
            _hasErrors = true;
            error.Write();
        }

        public void WriteErrors(IEnumerable<HotChocolate.IError> errors)
        {
            foreach (HotChocolate.IError error in errors)
            {
                _hasErrors = true;
                error.Write();
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            if (!_hasErrors)
            {
                Console.Write(
                    $"{_activityText} completed in " +
                    $"{_stopwatch.ElapsedMilliseconds} ms");
                if (_path is { })
                {
                    Console.WriteLine($"for {_path}.");
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
