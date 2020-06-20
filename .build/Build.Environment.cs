using System;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build : NukeBuild
{
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath AllSolutionFile => SourceDirectory / "All.sln";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
}

