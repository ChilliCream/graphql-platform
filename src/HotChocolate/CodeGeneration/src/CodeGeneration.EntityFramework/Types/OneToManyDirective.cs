using System;
using HotChocolate.CodeGeneration.EntityFramework.ModelBuilding;
using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    public class OneToManyDirective : IEntityConfiguringFieldDirective
    {
        public string? ForeignKey { get; set; }

        public string? InverseField { get; set; }

        public DeleteBehavior? OnDelete { get; set; }

        public StatementSyntax? Process(EntityBuilderContext context, ObjectField field)
        {
            IOutputType relationType = field.Type;
            if (!relationType.IsListType())
            {
                throw new Exception("Should be a list type");
            }

            Action<EntityBuilderContext> postProcessor = ctx =>
            {
                LocalDeclarationStatementSyntax? randomStatement = LocalDeclarationStatement(
                    VariableDeclaration(
                        IdentifierName(
                            Identifier(
                                TriviaList(),
                                SyntaxKind.VarKeyword,
                                "var",
                                "var",
                                TriviaList())))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier("x"))
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(field.Name.Value)))))));

                ctx.EntityConfigurerStatements.Add(randomStatement);


                // TODO: Prob need to index this by type name as a string,
                // then now here we can get the other side of the relationship,
                // and as such the primary key we've determined for it
                // We've got the full picture!
                bool lol = true;
                //var relation = ctx.ModelBuilderContext.EntityBuilderContexts[relationType.ElementType().NamedType()];
            };

            context.ModelBuilderContext.PostProcessors.Add((context.ObjectType, postProcessor));

            return null;
        }
    }

    public class OneToManyDirectiveType : DirectiveType<OneToManyDirective>
    {

        protected override void Configure(IDirectiveTypeDescriptor<OneToManyDirective> descriptor)
        {
            descriptor
                .Name("oneToMany")
                .Location(DirectiveLocation.FieldDefinition);

            descriptor
                .Argument(t => t.ForeignKey)
                .Description(
                    "The name of the field to use for the foreign key in this relationship. " +
                    "If none is provided, one will be derived.")
                .Type<StringType>();

            descriptor
                .Argument(t => t.InverseField)
                .Description("The name of the field that navigates back to the current type (if any).")
                .Type<StringType>();

            descriptor
                .Argument(t => t.OnDelete)
                .Description("The behavior to use when this principal entity is deleted.")
                .Type<EnumType<DeleteBehavior>>();
        }
    }

    public enum DeleteBehavior
    {
        ClientSetNull = 0,
        Restrict = 1,
        SetNull = 2,
        Cascade = 3,
        ClientCascade = 4,
        NoAction = 5,
        ClientNoAction = 6
    }
}
