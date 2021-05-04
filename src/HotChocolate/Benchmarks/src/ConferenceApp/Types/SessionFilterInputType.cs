using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Data.Filters;

namespace HotChocolate.ConferencePlanner.Types
{
    public class SessionFilterInputType : FilterInputType<Session>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Session> descriptor)
        {
            descriptor.Ignore(t => t.Id);
            descriptor.Ignore(t => t.TrackId); // todo : fix nullability issue with the descriptor.
        }
    }
}