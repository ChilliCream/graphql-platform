using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public class QueryError
       : IError
    {
        public QueryError(string message, params ErrorProperty[] extensions)
            : this(message, null, null, extensions)
        {
        }

        public QueryError(string message, Path path,
            params ErrorProperty[] extensions)
            : this(message, path, null, extensions)
        {
        }

        public QueryError(
            string message, IReadOnlyCollection<Location> locations,
            params ErrorProperty[] extensions)
            : this(message, null, locations, extensions)
        {
        }

        public QueryError(string message, Path path,
            IReadOnlyCollection<Location> locations,
            params ErrorProperty[] extensions)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            Message = message;
            Path = path?.ToCollection();
            Locations = locations;

            if (extensions?.Length > 0)
            {
                Extensions = extensions.ToDictionary(p => p.Name, p => p.Value);
            }
        }

        [JsonProperty("message", Order = 0)]
        public string Message { get; }

        [JsonProperty("path",
            Order = 2,
            NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyCollection<string> Path { get; }

        [JsonProperty("locations",
           Order = 3,
           NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyCollection<Location> Locations { get; }

        [JsonProperty("extensions",
            Order = int.MaxValue,
            NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> Extensions { get; }

        public string Code => null;

        #region Factories

        public static QueryError CreateFieldError(
            string message,
            Path path,
            FieldNode fieldSelection)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            return new QueryError(
                message,
                path,
                ConvertLocation(fieldSelection.Location));
        }

        public static QueryError CreateFieldError(
            string message,
            FieldNode fieldSelection)
        {
            return CreateFieldError(message, null, fieldSelection);
        }

        public static QueryError CreateArgumentError(
            string message,
            Path path,
            FieldNode fieldSelection,
            string argumentName)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (string.IsNullOrEmpty(argumentName))
            {
                throw new ArgumentException(
                    "The argument name mustn't be null or empty.",
                    nameof(argumentName));
            }

            return new QueryError(
                message,
                path,
                ConvertLocation(fieldSelection.Location),
                new ErrorProperty("argumentName", argumentName));
        }

        public static QueryError CreateArgumentError(
            string message,
            Path path,
            ArgumentNode argument)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            return new QueryError(
                message,
                path,
                ConvertLocation(argument.Location));
        }

        public static QueryError CreateArgumentError(
            string message,
            ArgumentNode argument)
        {
            return CreateArgumentError(message, null, argument);
        }

        public static QueryError CreateVariableError(
            string message,
            string variableName)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    "The error message mustn't be null or empty.",
                    nameof(message));
            }

            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentException(
                    "The variable name mustn't be null or empty.",
                    nameof(variableName));
            }

            return new QueryError(
                message,
                new ErrorProperty(nameof(variableName), variableName));
        }

        protected static Location[] ConvertLocation(
            Language.Location tokenLocation)
        {
            if (tokenLocation == null)
            {
                return null;
            }

            return new[]
            {
                new Location(
                    tokenLocation.StartToken.Line,
                    tokenLocation.StartToken.Column)
            };
        }

        protected static IReadOnlyCollection<Location> CreateLocations(
            params Language.ISyntaxNode[] syntaxNodes)
        {
            if (syntaxNodes?.Length == 0)
            {
                return null;
            }

            return syntaxNodes.Select(t => new Location(
                t.Location.StartToken.Line,
                t.Location.StartToken.Column)).ToArray();
        }

        #endregion
    }
}
