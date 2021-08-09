using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class SchemaConventionsDirective : IModelConfiguringDirective
    {
        internal const string DirectiveName = "schemaConventions";

        internal string DbContextName { get; set; } = "AppDbContext";

        public bool UsePluralizedTableNames { get; set; } = true;

        public StatementSyntax AsConfigurationStatement()
        {
            throw new System.NotImplementedException("TODO: First example of a ");
        }
    }

    public class SchemaConventionsDirectiveType : DirectiveType<SchemaConventionsDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<SchemaConventionsDirective> descriptor)
        {
            descriptor
                .Name(SchemaConventionsDirective.DirectiveName)
                .Location(DirectiveLocation.Schema);

            descriptor
                .Argument(t => t.UsePluralizedTableNames)
                .Description(
                    "If true, table names will be pluralized (e.g. Users) " +
                    "rather than singular (e.g. User). Default: true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
