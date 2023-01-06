using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Authorization;

public sealed class AuthorizationContext
{
    public AuthorizationContext(ISchema schema, IServiceProvider services, IDictionary<string, object?> contextData, DocumentNode document, string documentId)
    {
        Schema = schema;
        Services = services;
        ContextData = contextData;
        Document = document;
        DocumentId = documentId;
    }

    public ISchema Schema { get; }

    public IServiceProvider Services { get; }

    public IDictionary<string, object?> ContextData { get; }

    public DocumentNode Document { get; }

    public string DocumentId { get; }
}
