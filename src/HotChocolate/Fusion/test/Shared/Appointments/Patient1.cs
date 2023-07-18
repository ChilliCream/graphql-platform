using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

public class Patient1 : IPatient
{
    [ID<Patient1>] public int Id { get; set;}

    [UsePaging]
    public IEnumerable<Appointment> Appointments => new List<Appointment>()
    {
        new Appointment(),
        new Appointment(),
        new Appointment(),
    };
}
