using Nuke.Common;

partial class Build : NukeBuild
{
    [Parameter] readonly string GitHubToken;
    [Parameter] readonly string SonarToken;
}
