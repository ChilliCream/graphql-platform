using System;
using System.Collections.Generic;

namespace Generator
{
    internal class BackgroundResolver
    {
        /// <summary>
        /// schema - String (optional) - inline GraphQL SDL schema definition
        /// schema-file - String(optional) - SDL schema definition file path relative to the scenario file
        /// test-data - Object(optional) - test data used for query execution and directives
        /// test-data-file - String(optional) - test data file path relative to the scenario file.File can be in either JSON or YAML format.
        /// </summary>
        public static Background Resolve(object value)
        {
            if (value == null)
            {
                return Background.Empty;
            }

            var background = value as Dictionary<object, object>;
            if (background == null)
            {
                throw new InvalidOperationException("Invalid background structure");
            }

            var schema = background.TryGet("schema", string.Empty);
            var schemaFile = background.TryGet("schema-file", string.Empty);
            var testData = background.TryGet("test-data", new object());
            var testDataFile = background.TryGet("test-data-file", string.Empty);

            return new Background( schema, schemaFile, testData, testDataFile);    
        }
    }
}
