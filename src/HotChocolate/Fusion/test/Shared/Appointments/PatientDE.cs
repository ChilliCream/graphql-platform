using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

public class PatientDE : IPatient
{
    [ID<PatientDE>] public int Id { get; set;}
}
