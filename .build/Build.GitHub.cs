using System;
using Nuke.Common;

partial class Build : NukeBuild
{
    [Parameter] readonly string GitHubToken;

    /// <summary>
    /// ChilliCream/hotchocolate
    /// </summary>
    [Parameter] readonly string GitHubRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");

    /// /// <summary>
    /// Unique identifier of your PR. Must correspond to the key of the PR in GitHub.
    /// E.G.: sonar.pullrequest.key=5
    /// </summary>
    [Parameter] readonly string GitHubPRNumber = Environment.GetEnvironmentVariable("HC_GITHUB_PR_NR");

    /// <summary>
    /// The name of your PR
    /// Ex: sonar.pullrequest.branch=feature/my-new-feature
    /// </summary>
    [Parameter] readonly string GitHubHeadRef = Environment.GetEnvironmentVariable("HC_GITHUB_HEAD_REF");


    /// <summary>
    /// The long-lived branch into which the PR will be merged.
    /// Default: master
    /// E.G.: sonar.pullrequest.base=master
    /// </summary>
    [Parameter] readonly string GitHubBaseRef = Environment.GetEnvironmentVariable("HC_GITHUB_BASE_REF");
}

