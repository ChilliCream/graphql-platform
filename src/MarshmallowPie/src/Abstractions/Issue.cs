using System;
using HotChocolate.Language;

namespace MarshmallowPie
{
    public class Issue
    {
        public Issue(
            string message,
            string file,
            Location location,
            IssueType type = IssueType.Information,
            ResolutionType resolution = ResolutionType.Open)
            : this(Guid.NewGuid(), null, message, file, location, type, resolution)
        {
        }

        public Issue(
            string? code,
            string message,
            string file,
            Location location,
            IssueType type = IssueType.Information,
            ResolutionType resolution = ResolutionType.Open)
            : this(Guid.NewGuid(), code, message, file, location, type, resolution)
        {
        }

        public Issue(
            Guid id,
            string? code,
            string message,
            string file,
            Location location,
            IssueType type,
            ResolutionType resolution)
        {
            Id = id;
            Code = code ?? "NONE";
            Message = message;
            File = file;
            Location = location;
            Type = type;
            Resolution = resolution;
        }

        public Guid Id { get; }

        public string Code { get; }

        public string Message { get; }

        public string File { get; }

        public Location Location { get; }

        public IssueType Type { get; }

        public ResolutionType Resolution { get; }
    }
}
