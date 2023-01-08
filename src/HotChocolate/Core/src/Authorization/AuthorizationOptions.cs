using System;
using HotChocolate.Types;

namespace HotChocolate.Authorization;

/// <summary>
/// The authorization options.
/// </summary>
public class AuthorizationOptions
{
    /// <summary>
    /// Gets or sets a delegate that can be used to prevent authorization
    /// directives from being applied to the node field.
    /// </summary>
    public Func<AuthorizeDirective, bool> SkipNodeFields { get; set; } = _ => false;

    /// <summary>
    /// Gets or sets a hook that can be used to apply authorization
    /// policies to the node and nodes field.
    /// </summary>
    public Action<IObjectFieldDescriptor>? ConfigureNodeFields { get; set; }

    /// <summary>
    /// Gets or sets a hook that can be used to apply authorization
    /// policies to the __type field.
    /// </summary>
    public Action<IObjectFieldDescriptor>? ConfigureTypeField { get; set; }

    /// <summary>
    /// Gets or sets a hook that can be used to apply authorization
    /// policies to the __schema field.
    /// </summary>
    public Action<IObjectFieldDescriptor>? ConfigureSchemaField { get; set; }
}
