using System;
using System.Collections.Generic;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public class ModelBuilderContext
    {
        public SchemaConventionsDirective Conventions { get; }

        public string Namespace { get; }

        public List<(ObjectType, Action<EntityBuilderContext>)> PostProcessors { get; }
            = new();

        public Dictionary<ObjectType, EntityBuilderContext> EntityBuilderContexts { get; }
            = new();

        public ModelBuilderContext(SchemaConventionsDirective conventions, string @namespace)
        {
            Conventions = conventions;
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
        }
    }
}
