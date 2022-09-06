using System;

namespace HotChocolate.Execution;

/// <summary>
/// The GraphQL request flags allow to limit the GraphQL executor.
/// </summary>
[Flags]
public enum GraphQLRequestFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Query operations are allowed.
    /// </summary>
    AllowQuery = 1,

    /// <summary>
    /// Mutation operations are allowed.
    /// </summary>
    AllowMutation = 2,

    /// <summary>
    /// Subscription operations are allowed.
    /// </summary>
    AllowSubscription = 4,

    /// <summary>
    /// Stream results are allowed.
    /// </summary>
    AllowStreams = 8,

    /// <summary>
    /// Query operations and stream results are allowed.
    /// </summary>
    AllowQueryAndStream = AllowQuery | AllowStreams,

    /// <summary>
    /// Everything is allowed.
    /// </summary>
    AllowEverything = AllowQuery | AllowMutation | AllowSubscription | AllowStreams
}
