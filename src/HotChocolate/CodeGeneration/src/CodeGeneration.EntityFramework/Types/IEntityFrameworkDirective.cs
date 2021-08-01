using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.CodeGeneration.EntityFramework.Types
{
    /// <summary>
    /// A marker interface for directives that will result in configuration
    /// of the model via an EntityTypeBuilder of T.
    /// </summary>
    public interface IEntityFrameworkDirective
    {
        object AsConfiguration();
    }
}
