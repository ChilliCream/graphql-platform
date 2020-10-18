using System.Collections.Generic;

namespace HotChocolate.Data.Projections
{
    public interface IProjectionVisitorContext
        : ISelectionVisitorContext
    {
        IList<IError> Errors { get; }
    }
}
