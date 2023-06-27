namespace HotChocolate.Fusion.Shared.Appointments;

public class Appointment
{
    public int Id { get; set; }
    public IPatient PatientId { get; set; } = null!;
}
