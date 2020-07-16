using System;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build : NukeBuild
{
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath AllSolutionFile => SourceDirectory / "All.sln";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
    AbsolutePath CoverageReportDirectory => OutputDirectory / "coberage-reports";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";

    string ChangelogFile => RootDirectory / "CHANGELOG.md";
    AbsolutePath StrawberryShakeNuSpec => SourceDirectory / "StrawberryShake" / "CodeGeneration" / "src" / "MSBuild" / "StrawberryShake.nuspec";
    AbsolutePath StarWarsTemplateNuSpec =>  RootDirectory / "templates" / "StarWars" / "HotChocolate.Templates.StarWars.nuspec";
    AbsolutePath EmptyServerTemplateNuSpec => RootDirectory / "templates" / "Server" / "HotChocolate.Templates.Server.nuspec";
}

