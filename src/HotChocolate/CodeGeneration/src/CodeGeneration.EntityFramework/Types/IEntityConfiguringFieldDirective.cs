using HotChocolate.CodeGeneration.EntityFramework.ModelBuilding;
using HotChocolate.Types;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    /// <summary>
    /// A marker interface for field-level directives that configure an entity.
    /// </summary>
    public interface IEntityConfiguringFieldDirective
    {
        /// <summary>
        /// A method that processes a directive.
        ///
        /// It can return a statement that will be immediately added to the entity configurer class,
        /// or, it can queue up some delayed process by adding it onto the context.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="field">The field that was annotated.</param>
        /// <returns></returns>
        StatementSyntax? Process(EntityBuilderContext context, ObjectField field);
    }
}
