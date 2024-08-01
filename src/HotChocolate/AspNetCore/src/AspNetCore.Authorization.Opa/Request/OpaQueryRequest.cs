namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaQueryRequest
{
    public OpaQueryRequest(
        Policy policy,
        OriginalRequest request,
        IPAndPort source,
        IPAndPort destination,
        object? extensions = null)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        Input = new OpaQueryRequestInput(policy, request, source, destination, extensions);
    }

    public OpaQueryRequestInput Input { get; }

    public sealed class OpaQueryRequestInput
    {
        public OpaQueryRequestInput(
            Policy policy,
            OriginalRequest request,
            IPAndPort source,
            IPAndPort destination,
            object? extensions)
        {
            Policy = policy;
            Request = request;
            Source = source;
            Destination = destination;
            Extensions = extensions;
        }

        public Policy Policy { get; }

        public OriginalRequest Request { get; }

        public IPAndPort Source { get; }

        public IPAndPort Destination { get; }

        public object? Extensions { get; }
    }
}
