namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// The message types of the graphql-ws protocol
/// </summary>
internal enum GraphQLWebSocketMessageType
{
    /// <summary>
    /// Default message type.
    /// </summary>
    None,
    /// <summary>
    /// Client sends this message after plain websocket connection to start the
    /// communication with the server
    ///
    /// The server will response only with
    /// <see cref="ConnectionAccept"/> + <see cref="KeepAlive"/> (if used) or
    /// <see cref="Error"/> to this message.
    ///
    /// <example>
    /// <code>
    /// {
    ///    "type": "connection_init"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    ConnectionInit,

    /// <summary>
    /// The server may responses with this message to the <see cref="ConnectionInit"/> from
    /// client indicates the server accepted the connection. May optionally include a payload.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///    "type": "connection_ack"
    /// }
    /// </code>
    /// </example>
    ConnectionAccept,

    /// <summary>
    /// The server may responses with this message to the <see cref="ConnectionInit"/> from
    /// client, indicates the server rejected the connection.
    /// It server also respond with this message in case of a parsing errors of the message
    /// (which does not disconnect the client, just ignore the message).
    /// <example>
    /// <code>
    /// {
    ///    "type": "connection_error"
    ///    "payload": {
    ///         "message": "Something went wrong"
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    ConnectionError,

    /// <summary>
    /// Server message that should be sent right after each `<see cref="ConnectionAccept"/>`
    /// processed and then periodically to keep the client connection alive.
    ///
    /// The client starts to consider the keep alive message only upon the first received
    /// keep alive message from the server.
    /// <example>
    /// <code>
    /// {
    ///    "type": "ka"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    KeepAlive,

    /// <summary>
    /// Client sends this message to terminate the connection.
    /// <example>
    /// <code>
    /// {
    ///    "type": "connection_terminate"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    ConnectionTerminate,
    /// <summary>
    /// Client sends this message to execute GraphQL operation
    /// <example>
    /// <code>
    /// {
    ///    "id": "a2f1d594-cf4b-4e2c-85f6-5238ad6a9f68",
    ///    "type": "start",
    ///    "payload": {
    ///         "query": "subscription Foo{ onFoo }",
    ///         "variables": null,
    ///         "operationName": "Foo"
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    Start,

    /// <summary>
    /// The server sends this message to transfer the GraphQL execution result from the
    /// server to the client, this message is a response for <see cref="Start"/> message.
    ///
    /// For each GraphQL operation send with `<see cref="Start"/>`, the server will respond
    /// with at least one `<see cref="Data"/>` message.
    /// <example>
    /// <code>
    /// {
    ///    "id": "a2f1d594-cf4b-4e2c-85f6-5238ad6a9f68",
    ///    "type": "data",
    ///    "payload": {
    ///         "data": { "foo": "bar" },
    ///         "errors": [{"message": "Something"}],
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    Data,

    /// <summary>
    /// Server sends this message upon a failing operation, before the GraphQL execution,
    /// usually due to GraphQL validation errors (resolver errors are part of
    /// <see cref="Data"/> message, and will be added as errors array)
    /// <example>
    /// <code>
    /// {
    ///    "id": "a2f1d594-cf4b-4e2c-85f6-5238ad6a9f68",
    ///    "type": "error",
    ///    "payload": {
    ///         "message": "Something",,
    ///    }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    Error,

    /// <summary>
    /// Server sends this message to indicate that a GraphQL operation is done, and no more
    /// data will arrive for the specific operation.
    /// <example>
    /// <code>
    /// {
    ///    "id": "a2f1d594-cf4b-4e2c-85f6-5238ad6a9f68",
    ///    "type": "complete"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    Complete,

    /// <summary>
    /// Client sends this message in order to stop a running GraphQL operation execution
    /// (for example: unsubscribe)
    /// <example>
    /// <code>
    /// {
    ///    "id": "a2f1d594-cf4b-4e2c-85f6-5238ad6a9f68",
    ///    "type": "stop"
    /// }
    /// </code>
    /// </example>
    /// </summary>
    Stop,
}
