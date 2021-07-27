using System;
using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Types.Relay;

namespace HotChocolate.ConferencePlanner.Sessions
{
    public record ScheduleSessionInput(
        [GlobalId(nameof(Session))]
        int SessionId,
        [GlobalId(nameof(Track))]
        int TrackId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime);
}