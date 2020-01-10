using System;

namespace MarshmallowPie
{
    public class Issue
    {
        public Issue(
            string code,
            string message,
            IssueType type,
            ResolutionType? resolution)
            : this(Guid.NewGuid(), code, message, type, resolution)
        {
        }

        public Issue(
            Guid id,
            string code,
            string message,
            IssueType type,
            ResolutionType? resolution)
        {
            Id = id;
            Code = code;
            Message = message;
            Type = type;
            Resolution = resolution;
        }

        public Guid Id { get; }

        public string Code { get; }

        public string Message { get; }

        public IssueType Type { get; }

        public ResolutionType? Resolution { get; }
    }
}
