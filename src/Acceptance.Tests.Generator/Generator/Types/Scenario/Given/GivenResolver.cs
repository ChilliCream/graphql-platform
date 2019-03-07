using System;
using System.Collections.Generic;

namespace Generator
{
    internal static class GivenResolver
    {
        /// <summary>
        /// query - String - the GraphQL query to execute an action against
        /// schema - String (optional) - inline GraphQL SDL schema definition
        /// schema-file - String (optional) - SDL schema definition file path relative to the scenario file
        /// test-data - Object (optional) - test data used for query execution and directives
        /// test-data-file - String (optional) - test data file path relative to the scenario file. File can be in either JSON or YAML format.
        /// </summary>
        internal static Given Resolve(object value)
        {
            var given = value as Dictionary<object, object>;
            if (given == null)
            {
                throw new InvalidOperationException("Invalid given structure");
            }

            var query = given["query"] as string;
            var schema = given.TryGet("schema", string.Empty);
            var schemaFile = given.TryGet("schema-file", string.Empty);
            var testData = given.TryGet("test-data", new object());
            var testDataFile = given.TryGet("test-data-file", string.Empty);

            return new Given(query, schema, schemaFile, testData, testDataFile);
        }
    }
}
