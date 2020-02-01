using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class FieldInfo
    {
        public NameString ResponseName { get; set; }

        public ObjectField Field { get; set; }

        public FieldNode Selection { get; set; }

        public List<FieldNode> Nodes { get; set; }

        public FieldDelegate Middleware { get; set; }

        public Path Path { get; set; }

        public Dictionary<NameString, ArgumentValue> Arguments { get; set; }

        public Dictionary<NameString, ArgumentVariableValue> VarArguments { get; set; }

        public List<FieldVisibility> Visibilities { get; set; }
    }
}
