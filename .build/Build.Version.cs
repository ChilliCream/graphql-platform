using System;
using Nuke.Common;
using Semver;

partial class Build
{
    [Parameter] readonly string SemVersion = "14.0.0-preview.build.0";

    [Parameter]
    string Version =>
        Semver.SemVersion
            .Parse(SemVersion, SemVersionStyles.Any)
            .WithoutPrereleaseOrMetadata()
            .ToString();
}
