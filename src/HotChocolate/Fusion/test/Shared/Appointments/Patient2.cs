using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

public class Patient2 : IPatient
{
    [ID<Patient2>]public int Id { get; set;}
}
