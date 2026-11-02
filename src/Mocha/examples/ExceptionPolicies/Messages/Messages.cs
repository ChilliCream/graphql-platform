namespace ExceptionPolicies.Messages;

public record ProcessPayment(string OrderId, decimal Amount);

public record ValidateOrder(string OrderId);

public record DeduplicateMessage(string MessageId);

public record CallExternalApi(string Url);

public record RefreshAuthToken(string Service);

public record ProcessBatch(string BatchId);

public record IngestTelemetry(string DeviceId);

public record HandlePoisonMessage(string Data);
