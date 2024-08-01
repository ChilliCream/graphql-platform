using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Patients;

[GraphQLName("Query")]
public class Patient1Query
{
    [NodeResolver]
    public Patient1 GetPatientById(int patientId)
        => new() { Id = patientId, Name = "Karl Kokoloko", };
}
