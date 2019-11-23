using System.Collections.Generic;
using System.Diagnostics;

namespace StrawberryShake.Tools
{
    public class JsonConsoleOutputActivity
        : IActivity
    {
        private readonly JsonConsoleOutputData _data;
        private readonly string _activityText;
        private readonly string? _path;
        private readonly Stopwatch _stopwatch;
        private bool _hasErrors;

        public JsonConsoleOutputActivity(
            JsonConsoleOutputData data,
            string activityText,
            string? path)
        {
            _data = data;
            _activityText = activityText;
            _path = path;
            _stopwatch = Stopwatch.StartNew();
        }

        public void WriteError(HotChocolate.IError error)
        {
            _hasErrors = true;
            _data.Errors.Add(new JsonConsoleOutputErrorData(error));
        }

        public void WriteErrors(IEnumerable<HotChocolate.IError> errors)
        {
            foreach (HotChocolate.IError error in errors)
            {
                _hasErrors = true;
                _data.Errors.Add(new JsonConsoleOutputErrorData(error));
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            if (!_hasErrors)
            {
                _data.Activities.Add(new JsonConsoleOutputActivityData(
                    _activityText,
                    _path,
                    _stopwatch.Elapsed));
            }
        }
    }
}
