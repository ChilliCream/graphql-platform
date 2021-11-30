using Nuke.Common.Git;
using Nuke.Common.Tools.GitVersion;

partial class Build
{
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = Net60)] readonly GitVersion GitVersion;
}

