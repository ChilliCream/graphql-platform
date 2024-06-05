using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// The <see cref="InterfaceFieldDefinition"/> contains the settings
/// to create a <see cref="InterfaceField"/>.
/// </summary>
public class InterfaceFieldDefinition : OutputFieldDefinitionBase
{
    private List<IParameterExpressionBuilder>? _expressionBuilders;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceFieldDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
    /// </summary>
    public InterfaceFieldDefinition(
        string name,
        string? description = null,
        TypeReference? type = null)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        Type = type;
    }

    /// <summary>
    /// Gets the interface member to which this field is bound to.
    /// </summary>
    public MemberInfo? Member { get; set; }

    /// <summary>
    /// A list of parameter expression builders that shall be applied when compiling
    /// the resolver or when arguments are inferred from a method.
    /// </summary>
    public IList<IParameterExpressionBuilder> ParameterExpressionBuilders
    {
        get
        {
            return _expressionBuilders ??= [];
        }
    }

    /// <summary>
    /// A list of parameter expression builders that shall be applied when compiling
    /// the resolver or when arguments are inferred from a method.
    /// </summary>
    internal IReadOnlyList<IParameterExpressionBuilder> GetParameterExpressionBuilders()
    {
        if (_expressionBuilders is null)
        {
            return Array.Empty<IParameterExpressionBuilder>();
        }

        return _expressionBuilders;
    }

    internal void CopyTo(InterfaceFieldDefinition target)
    {
        base.CopyTo(target);

        if (_expressionBuilders is { Count: > 0, })
        {
            target._expressionBuilders = [.._expressionBuilders,];
        }

        target.Member = Member;
    }

    internal void MergeInto(InterfaceFieldDefinition target)
    {
        base.MergeInto(target);

        if (_expressionBuilders is { Count: > 0, })
        {
            target._expressionBuilders ??= [];
            target._expressionBuilders.AddRange(_expressionBuilders);
        }

        if (Member is not null)
        {
            target.Member = Member;
        }
    }
}
