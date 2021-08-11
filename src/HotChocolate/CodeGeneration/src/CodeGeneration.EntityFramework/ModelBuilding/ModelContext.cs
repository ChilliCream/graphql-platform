using System;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public class ModelContext
    {
        public SchemaConventionsDirective Conventions { get; }

        public string Namespace { get; }

        public ObjectType ObjectType { get; }

        public string? ModelName { get; set; }

        public string RequiredModelName
            => ModelName ?? throw new Exception("Model name is required");

        public string? ModelNamePluralized { get; set; }

        public string RequiredModelNamePluralized
            => ModelNamePluralized ?? throw new Exception("Model name pluralized is required");

        public string? ModelConfigurerName => $"{ModelName}Configurer";

        public string RequiredModelConfigurerName
            => ModelConfigurerName ?? throw new Exception("Model configurer name is required");

        public bool IsBackedByTable { get; set; }

        public string? TableName { get; set; }

        public TableDirective? TableDirective { get; set; }

        public PrimaryKeyField[]? PrimaryKey { get; set; }

        public ClassDeclarationSyntax? ModelClass { get; set; }

        public ClassDeclarationSyntax? ModelConfigurerClass { get; set; }

        public UsingDirectiveSyntax[] ModelConfigurerUsings { get; } = SyntaxConstants.ModelConfigurerUsings;

        public ModelContext(SchemaConventionsDirective conventions, string @namespace, ObjectType objectType)
        {
            Conventions = conventions;
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
        }
    }
}
