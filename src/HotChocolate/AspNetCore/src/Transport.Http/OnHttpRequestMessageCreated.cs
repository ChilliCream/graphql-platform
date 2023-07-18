using System.Net.Http;

namespace HotChocolate.Transport.Http;

public delegate void OnHttpRequestMessageCreated(
    OperationRequest request, 
    HttpRequestMessage requestMessage);