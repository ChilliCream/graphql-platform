using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StrawberryShake.Tools
{
    public class DefaultConsoleOutput
        : IConsoleOutput
    {
        public IDisposable WriteCommand() => new DummyConsoleContext();

        public IActivity WriteActivity(string text, string? path = null)
            => new DefaultConsoleOutputActivity(text, path);

        public void WriteFileCreated(string fileName)
        {
        }

        private class DummyConsoleContext : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public class JsonConsoleOutput
        : IConsoleOutput
    {
        private readonly  JsonConsoleOutputData _data =
                new JsonConsoleOutputData();

        public IDisposable WriteCommand()
        {
            throw new NotImplementedException();
        }

        public IActivity WriteActivity(string text, string path = null)
        {

        }

        public void WriteFileCreated(string fileName)
        {
            throw new NotImplementedException();
        }
    }

    public class JsonConsoleOutputData
    {
        public List<JsonConsoleOutputActivityData> Activities { get; } =
            new List<JsonConsoleOutputActivityData>();

        public List<JsonConsoleOutputErrorData> Errors { get; } =
            new List<JsonConsoleOutputErrorData>();

        public  List<string> CreatedFiles { get; } =
            new List<string>();
    }

    public class JsonConsoleOutputActivityData
    {
        public JsonConsoleOutputActivityData(string text, string? path, TimeSpan time)
        {
            Text = text;
            Path = path;
            Time = time;
        }

        public string Text { get; }

        public  string? Path { get; }

        public TimeSpan Time { get; }
    }

    public class JsonConsoleOutputErrorData
    {
        public JsonConsoleOutputErrorData(HotChocolate.IError error)
        {
            Message = error.Message;
            Code = error.Code;

            if (error.Extensions is { } && error.Extensions.ContainsKey("fileName"))
            {
                FileName = $"{Path.GetFullPath((string)error.Extensions["fileName"])}";
            }

            if (error.Locations is { } && error.Locations.Count > 0)
            {
                Location = error.Locations[0];
            }
        }

        public string Message { get; }

        public string Code { get; }

        public string? FileName { get; }

        public HotChocolate.Location? Location { get; }
    }

    public class JsonConsoleOutputActivity
        : IActivity
    {
        private readonly JsonConsoleOutputData _data;
        private readonly string _activityText;
        private readonly string _path;
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
