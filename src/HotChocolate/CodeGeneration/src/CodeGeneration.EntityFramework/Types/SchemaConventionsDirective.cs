using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class SchemaConventionsDirective : IModelConfiguringDirective
    {
        internal const string DirectiveName = "schemaConventions";

        internal string DbContextName { get; set; } = "AppDbContext";

        public StatementSyntax AsConfigurationStatement()
        {
            Block()
        }
    }

    public class SchemaConventionsDirectiveType : DirectiveType<SchemaConventionsDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<SchemaConventionsDirective> descriptor)
        {
            descriptor
                .Name(SchemaConventionsDirective.DirectiveName)
                .Location(DirectiveLocation.Schema);
        }
    }
}
