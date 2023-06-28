using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Appointments;

[GraphQLName("Query")]
public class AppointmentQuery
{
    [UsePaging]
    public IEnumerable<Appointment> Appointments()
    {
        yield return new Appointment { Id = 1, Patient = new Patient1 { Id = 1 } };
        yield return new Appointment { Id = 2, Patient = new Patient2 { Id = 2 } };
    }

    [NodeResolver]
    public Appointment? GetAppointmentById(int appointmentId)
    {
        if (appointmentId == 1)
        {
            return new Appointment { Id = 1, Patient = new Patient1 { Id = 1 } }; ;
        }
        else if (appointmentId == 2)
        {
            return new Appointment { Id = 2, Patient = new Patient2 { Id = 2 } };
        }
        else
        {
            return null;
        }
    }
}
