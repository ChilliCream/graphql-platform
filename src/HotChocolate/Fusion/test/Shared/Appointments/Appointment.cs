namespace HotChocolate.Fusion.Shared.Appointments;

public class Appointment
{
    public int Id { get; set; }
    public IPatient Patient { get; set; } = null!;
}
