using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration.Validation
{
    public class TypeValidationTestBase
    {
        public static void ExpectValid(string schema)
        {
            SchemaBuilder.New()
                .AddDocumentFromString(schema)
                .Use(next => context => Task.CompletedTask)
                .Create();
        }

        public static void ExpectError(string schema, params Action<ISchemaError>[] errorAssert)
        {
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(schema)
                    .Use(next => context => Task.CompletedTask)
                    .Create();
            }
            catch (SchemaException ex)
            {
                Assert.NotEmpty(ex.Errors);

                if (errorAssert.Length > 0)
                {
                    Assert.Collection(ex.Errors, errorAssert);
                }

                Serialize(ex.Errors).MatchSnapshot();
            }
        }

        private static List<Dictionary<string, object>> Serialize(IEnumerable<ISchemaError> errors)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (ISchemaError error in errors)
            {
                list.Add(Serialize(error));
            }

            return list;
        }

        private static Dictionary<string, object> Serialize(ISchemaError error)
        {
            var dict = new Dictionary<string, object>();
            dict[nameof(error.Message)] = error.Message;
            dict[nameof(error.Code)] = error.Code;
            dict[nameof(error.TypeSystemObject)] = Serialize(error.TypeSystemObject);
            dict[nameof(error.Path)] = error.Path;

            dict[nameof(error.SyntaxNodes)] = error.SyntaxNodes is { }
                ? error.SyntaxNodes.Select(t => t.ToString()).ToList()
                : null;

            dict[nameof(error.Extensions)] = error.Extensions is { }
                ? Serialize(error.Extensions)
                : null;

            dict[nameof(error.Exception)] = error.Exception is { }
                ? error.Exception.Message
                : null;

            return dict;
        }

        private static IDictionary<string, object> Serialize(
            IReadOnlyDictionary<string, object> extensions)
        {
            var dict = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> item in extensions.OrderBy(t => t.Key))
            {
                dict[item.Key] = Serialize(item.Value);
            }

            return dict;
        }

        private static string Serialize(object o)
        {
            if (o is null)
            {
                return null;
            }
            else if (o is IField f)
            {
                return f.Name;
            }
            else if (o is INamedType n)
            {
                return n.Name.HasValue ? n.Name.Value : n.GetType().FullName;
            }
            else
            {
                return o.ToString();
            }
        }
    }
}
