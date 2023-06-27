using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

public class PatientCH : IPatient
{
    [ID<PatientCH>]public int Id { get; set;}
}
