using System;
using HotChocolate.Types;

namespace HotChocolate.Authorization;

public class AuthorizationOptions
{
    public Func<AuthorizeDirective, bool> SkipNodeFields { get; set; } = _ => false;

    public Action<IObjectFieldDescriptor>? ConfigureNodeFields { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureTypeField { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureSchemaField { get; set; }
}
