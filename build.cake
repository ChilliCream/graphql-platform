#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var testOutputDir = Directory("./testoutput");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    //CleanDirectory(buildDir);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration
    };

    DotNetCoreBuild("./src", settings);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{

});

Task("Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    int i = 0;

    var settings = new DotNetCoreTestSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append($"/p:CollectCoverage=true")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/{i++}\""),
        NoBuild = true,
        NoRestore = true
    };

    DotNetCoreTest("./src/AspNetCore.Tests", settings);
    DotNetCoreTest("./src/Core.Tests", settings);
    DotNetCoreTest("./src/Language.Tests", settings);
    DotNetCoreTest("./src/Runtime.Tests", settings);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
