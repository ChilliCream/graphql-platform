using Grpc.Core;

namespace HotChocolate.AspNetCore.Grpc
{
    public static class GrpcHelpers
    {
        public static RpcException CreateRpcException(StatusCode statusCode, string message, Metadata? trailers = default)
            => new RpcException(new Status(statusCode, message), trailers ?? new Metadata());
    }
}
