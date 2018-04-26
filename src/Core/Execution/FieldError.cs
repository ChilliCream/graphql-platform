using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class FieldError
        : QueryError
    {
        public FieldError(string message, FieldNode fieldSelection)
            : base(message)
        {
            FieldName = fieldSelection.Name.Value;
            Locations = new[]
            {
                new Location(
                    fieldSelection.Location.StartToken.Line,
                    fieldSelection.Location.StartToken.Column)
            };
        }

        public string FieldName { get; }
        public IReadOnlyCollection<Location> Locations { get; }
    }
}
