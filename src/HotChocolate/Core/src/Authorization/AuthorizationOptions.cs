using System;
using HotChocolate.Types;

namespace HotChocolate.Authorization;

public class AuthorizationOptions
{
    public Func<AuthorizeDirective, bool> SkipNodeFields { get; set; } = _ => false;
    public bool SkipTypeNameField { get; set; } = true;

    public bool SkipSchemaField { get; set; }

    public bool SkipTypeField { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureNodeFields { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureTypeNameField { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureTypeField { get; set; }

    public Action<IObjectFieldDescriptor>? ConfigureSchemaField { get; set; }
}
