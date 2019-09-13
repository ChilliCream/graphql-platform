using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    [Obsolete("Use ErrorBuilder instead.")]
    public class QueryError
       : IError
    {
        [JsonIgnore]
        private ImmutableDictionary<string, object> _extensions;

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
            string message, IReadOnlyList<Location> locations,
            params ErrorProperty[] extensions)
            : this(message, (Path)null, locations, extensions)
        {
        }

        public QueryError(string message, Path path,
            IReadOnlyList<Location> locations,
            params ErrorProperty[] extensions)
            : this(message, path?.ToCollection(), locations,
                extensions?.ToImmutableDictionary(p => p.Name, p => p.Value))
        {
        }

        internal QueryError(string message,
            IReadOnlyList<object> path,
            IReadOnlyList<Location> locations,
            ImmutableDictionary<string, object> extensions)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    CoreResources.QueryError_MessageIsNull,
                    nameof(message));
            }

            Message = message;
            Path = path;
            Locations = locations;

            if (extensions != null && extensions.Count > 0)
            {
                _extensions = extensions;
            }
        }

        private QueryError(IDictionary<string, object> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            Message = (string)dict["message"];

            if (dict.TryGetValue("extensions", out object obj)
                && obj is IDictionary<string, object> extensions)
            {
                _extensions = ImmutableDictionary<string, object>
                    .Empty.AddRange(extensions);
            }

            if (dict.TryGetValue("path", out obj)
                && obj is IList<object> path)
            {
                Path = path.OfType<string>().ToArray();
            }

            if (dict.TryGetValue("locations", out obj)
                && obj is IList<object> locations)
            {
                var locs = new List<Location>();
                foreach (IDictionary<string, object> loc in locations
                    .OfType<IDictionary<string, object>>())
                {
                    locs.Add(new Location(
                        Convert.ToInt32(loc["line"]),
                        Convert.ToInt32(loc["column"])));
                }
                Locations = locs.AsReadOnly();
            }
        }


        [JsonProperty("message", Order = 0)]
        public string Message { get; }

        [JsonProperty("path",
            Order = 2,
            NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<object> Path { get; }

        [JsonProperty("locations",
           Order = 3,
           NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyList<Location> Locations { get; }

        [JsonProperty("extensions",
            Order = int.MaxValue,
            NullValueHandling = NullValueHandling.Ignore)]
        public IReadOnlyDictionary<string, object> Extensions => _extensions;

        [JsonIgnore]
        public string Code
        {
            get
            {
                if (_extensions != null
                    && _extensions.TryGetValue("code", out object o))
                {
                    return o.ToString();
                }
                return null;
            }
            private set
            {
                if (value == null)
                {
                    _extensions = _extensions.Remove("code");
                    if (_extensions.Count == 0)
                    {
                        _extensions = null;
                    }
                }
                else
                {
                    if (_extensions == null)
                    {
                        _extensions = ImmutableDictionary<string, object>.Empty;
                    }
                    _extensions = _extensions.SetItem("code", value);
                }
            }
        }

        public Exception Exception { get; internal set; }

        #region Factories

        public static QueryError CreateFieldError(
            string message,
            Path path,
            FieldNode fieldSelection)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException(
                    CoreResources.QueryError_MessageIsNull,
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
                    CoreResources.QueryError_MessageIsNull,
                    nameof(message));
            }

            if (fieldSelection == null)
            {
                throw new ArgumentNullException(nameof(fieldSelection));
            }

            if (string.IsNullOrEmpty(argumentName))
            {
                throw new ArgumentException(
                    CoreResources.QueryError_ArgumentIsNull,
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
                    CoreResources.QueryError_MessageIsNull,
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
                    CoreResources.QueryError_MessageIsNull,
                    nameof(message));
            }

            if (string.IsNullOrEmpty(variableName))
            {
                throw new ArgumentException(
                    CoreResources.QueryError_VariableIsNull,
                    nameof(variableName));
            }

            return new QueryError(
                message,
                new ErrorProperty(nameof(variableName), variableName));
        }

        public static QueryError FromDictionary(
            IDictionary<string, object> dict)
        {
            return new QueryError(dict);
        }

        protected internal static Location[] ConvertLocation(
            Language.Location tokenLocation)
        {
            if (tokenLocation == null)
            {
                return null;
            }

            return new[]
            {
                new Location(
                    tokenLocation.Line,
                    tokenLocation.Column)
            };
        }

        protected internal static IReadOnlyList<Location> CreateLocations(
            params ISyntaxNode[] syntaxNodes)
        {
            if (syntaxNodes?.Length == 0)
            {
                return null;
            }

            return syntaxNodes.Select(t => new Location(
                t.Location.Line,
                t.Location.Column)).ToArray();
        }

        public IError WithMessage(string message)
        {
            return new QueryError(message, Path, Locations, _extensions);
        }

        public IError WithCode(string code)
        {
            return new QueryError(Message, Path, Locations, _extensions)
            {
                Code = code
            };
        }

        public IError WithPath(Path path)
        {
            return new QueryError(
                Message,
                path?.ToCollection(),
                Locations,
                _extensions);
        }

        public IError WithPath(IReadOnlyList<object> path)
        {
            return new QueryError(
                Message,
                path,
                Locations,
                _extensions);
        }

        public IError WithLocations(IReadOnlyList<Location> locations)
        {
            return new QueryError(Message, Path, locations, _extensions);
        }

        public IError WithExtensions(
            IReadOnlyDictionary<string, object> extensions)
        {
            return new QueryError(Message, Path, Locations,
                extensions?.ToImmutableDictionary());
        }

        public IError AddExtension(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ImmutableDictionary<string, object> dict = _extensions == null
                ? ImmutableDictionary<string, object>.Empty
                : _extensions.ToImmutableDictionary();

            return new QueryError(Message, Path, Locations,
                dict.SetItem(key, value));
        }

        public IError RemoveExtension(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ImmutableDictionary<string, object> dict = _extensions == null
                ? ImmutableDictionary<string, object>.Empty
                : _extensions.ToImmutableDictionary();

            return new QueryError(Message, Path, Locations,
                dict.Remove(key));
        }

        public IError WithException(Exception exception)
        {
            return new QueryError(Message, Path, Locations, _extensions)
            {
                Exception = exception
            };
        }

        public IError RemoveException()
        {
            return new QueryError(Message, Path, Locations, _extensions)
            {
                Exception = null
            };
        }

        #endregion
    }
}
