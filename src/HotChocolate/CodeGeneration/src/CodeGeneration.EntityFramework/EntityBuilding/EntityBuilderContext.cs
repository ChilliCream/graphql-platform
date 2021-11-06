using System;
using System.Collections.Generic;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.ModelBuilding
{
    public class EntityBuilderContext
    {
        public ModelBuilderContext ModelBuilderContext { get; }

        public ObjectType ObjectType { get; }

        public string? EntityName { get; set; }

        public string RequiredEntityName
            => EntityName ?? throw new Exception("Entity name is required");

        public string? EntityNamePluralized { get; set; }

        public string RequiredEntityNamePluralized
            => EntityNamePluralized ?? throw new Exception("Entity name pluralized is required");

        public string? EntityConfigurerName => $"{EntityName}Configurer";

        public string RequiredEntityConfigurerName
            => EntityConfigurerName ?? throw new Exception("Entity configurer name is required");

        public bool IsBackedByTable { get; set; }

        public string? TableName { get; set; }

        public TableDirective? TableDirective { get; set; }

        public string? PrimaryKeyName { get; set; }

        public PrimaryKeyColumn[]? PrimaryKeyColumns { get; set; }

        public PrimaryKeyColumn[] RequiredPrimaryKeyColumns
            => PrimaryKeyColumns ?? throw new Exception("Primary key is required");

        public ClassDeclarationSyntax? EntityClass { get; set; }

        public ClassDeclarationSyntax? EntityConfigurerClass { get; set; }

        public UsingDirectiveSyntax[] EntityConfigurerUsings { get; } = SyntaxConstants.ModelConfigurerUsings;
        public List<StatementSyntax> EntityConfigurerStatements { get; } = new();

        public EntityBuilderContext(
            ModelBuilderContext modelBuilderContext,
            ObjectType objectType)
        {
            ModelBuilderContext = modelBuilderContext ?? throw new ArgumentNullException(nameof(modelBuilderContext));
            ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));

            modelBuilderContext.EntityBuilderContexts[ObjectType] = this;
        }
    }
}
