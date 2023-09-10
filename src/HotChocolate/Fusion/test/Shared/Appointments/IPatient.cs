using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

public interface IPatient
{
    [ID] public int Id { get; }
}
