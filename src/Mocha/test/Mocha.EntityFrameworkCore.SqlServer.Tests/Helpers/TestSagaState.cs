using System.Text.Json.Serialization;
using Mocha.Sagas;

namespace Mocha.EntityFrameworkCore.SqlServer.Tests.Helpers;

public sealed class TestSagaState : SagaStateBase
{
    public TestSagaState() : base() { }

    public TestSagaState(Guid id, string state) : base(id, state) { }

    public string? Data { get; set; }
}

[JsonSerializable(typeof(TestSagaState))]
internal partial class TestSagaStateJsonContext : JsonSerializerContext;
