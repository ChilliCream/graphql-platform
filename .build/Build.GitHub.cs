using System;
using Nuke.Common;

partial class Build : NukeBuild
{
    [Parameter] readonly string GitHubToken;
    [Parameter] readonly string GitHubRef = Environment.GetEnvironmentVariable("GITHUB_REF");
    [Parameter] readonly string GitHubRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
    [Parameter] readonly string GitHubHeadRef = Environment.GetEnvironmentVariable("HC_GITHUB_HEAD_REF");
    [Parameter] readonly string GitHubBaseRef = Environment.GetEnvironmentVariable("HC_GITHUB_BASE_REF");
}

