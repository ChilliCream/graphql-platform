using HotChocolate.CodeGeneration.EntityFramework.ModelBuilding;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    /// <summary>
    /// A marker interface for type-level directives that configure an entity.
    /// </summary>
    public interface IEntityConfiguringDirective
    {
        /// <summary>
        /// A method that processes a directive.
        ///
        /// It can return a statement that will be immediately added to the entity configurer class,
        /// or, it can queue up some delayed process by adding it onto the context.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        StatementSyntax? Process(EntityBuilderContext context);
    }
}
