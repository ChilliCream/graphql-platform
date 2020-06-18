using Nuke.Common;

partial class Build : NukeBuild
{
    [Parameter] readonly string SonarToken;
    [Parameter] readonly string SonarServer = "https://sonarcloud.io";
}
