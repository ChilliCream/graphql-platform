using System;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitVersion;

partial class Build : NukeBuild
{
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion(Framework = "net5.0")] readonly GitVersion GitVersion;
}

