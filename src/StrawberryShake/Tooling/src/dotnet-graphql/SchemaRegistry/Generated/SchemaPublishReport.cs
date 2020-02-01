using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Tools.SchemaRegistry
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class SchemaPublishReport
        : ISchemaPublishReport
    {
        public SchemaPublishReport(
            IEnvironment environment, 
            ISchemaVersion schemaVersion)
        {
            Environment = environment;
            SchemaVersion = schemaVersion;
        }

        public IEnvironment Environment { get; }

        public ISchemaVersion SchemaVersion { get; }
    }
}
