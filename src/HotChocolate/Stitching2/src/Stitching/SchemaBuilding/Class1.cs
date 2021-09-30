using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class Class1
    {
    }

    public class NodeIdentifier
    {

    }

    public class TypeIdentifier : NodeIdentifier
    {

    }

    public class FieldIdentified : NodeIdentifier
    {
            
    }


    public class SchemaConfiguration
    {
        public HashSet<OperationType> IncludedOperations { get; } = new();

        public HashSet<string> IgnoreDirectiveDeclarations { get; } = new();
    }
}
