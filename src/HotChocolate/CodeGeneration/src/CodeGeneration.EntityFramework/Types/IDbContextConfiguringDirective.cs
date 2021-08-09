using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    /// <summary>
    /// A marker interface for directives that will result in configuration
    /// of the DbContext via the OnModelConfiguring method.
    /// </summary>
    public interface IDbContextConfiguringDirective
    {
        StatementSyntax AsConfigurationStatement();
    }
}
