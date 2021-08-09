using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    /// <summary>
    /// A marker interface for directives that will result in configuration
    /// of the model via an EntityTypeBuilder of T.
    /// </summary>
    public interface IModelConfiguringDirective
    {
        StatementSyntax AsConfigurationStatement();
    }
}
