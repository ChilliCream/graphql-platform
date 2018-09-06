#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin "nuget:?package=Cake.Sonar"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var sonarLogin = Argument("sonarLogin", default(string));
var sonarBranch = Argument("sonarBranch", default(string));
var sonarBranchTitle = Argument("sonarBranchTitle", default(string));
var packageVersion = Argument("packageVersion", default(string));


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
var testOutputDir = Directory("./testoutput");
var publishOutputDir = Directory("./artifacts");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("EnvironmentSetup")
    .Does(() =>
{
    if(string.IsNullOrEmpty(packageVersion))
    {
        packageVersion = EnvironmentVariable("CIRCLE_TAG")
            ?? EnvironmentVariable("Version");
    }
    Environment.SetEnvironmentVariable("Version", packageVersion);

    if(string.IsNullOrEmpty(sonarBranch))
    {
        sonarBranch = EnvironmentVariable("CIRCLE_PR_NUMBER");
        sonarBranchTitle = EnvironmentVariable("CIRCLE_PULL_REQUEST");
    }

    if(string.IsNullOrEmpty(sonarLogin))
    {
        sonarLogin = EnvironmentVariable("SONAR_TOKEN");
    }
});

Task("Clean")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    DotNetCoreClean("./src");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./src");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    };
    DotNetCoreBuild("./src", settings);
});


Task("Build-Debug")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug",
        NoRestore = true
    };
    DotNetCoreBuild("./src", settings);
});

Task("Publish")
    .IsDependentOn("Build")
    .Does(() =>
{
    var settings = new DotNetCorePackSettings
    {
        Configuration = configuration,
        OutputDirectory = publishOutputDir,
        NoRestore = true,
        NoBuild = true,
        IncludeSource = true,
        IncludeSymbols = true,
        ArgumentCustomization = args =>
        {
            var a = args;

            if(!string.IsNullOrEmpty(packageVersion))
            {
                a = a.Append($"/p:PackageVersion={packageVersion}");
                a = a.Append($"/p:VersionPrefix={packageVersion.Split('-').First()}");
            }

            return a;
        }
    };
    DotNetCorePack("./src", settings);
});

Task("Tests")
    .IsDependentOn("Build-Debug")
    .Does(() =>
{
    int i = 0;
    var settings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => args.Append($"/p:CollectCoverage=true")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/{i++}\"")
    };

    DotNetCoreTest("./src/Language.Tests", settings);
    DotNetCoreTest("./src/Runtime.Tests", settings);
    //DotNetCoreTest("./src/Core.Tests", settings);
    DotNetCoreTest("./src/AspNetCore.Tests", settings);
});

Task("SonarBegin")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    SonarBegin(new SonarBeginSettings{
        Url = "https://sonarcloud.io",
        Login = sonarLogin,
        Key = "HotChocolate",
        Organization = "chillicream",
        VsTestReportsPath = "**/*.trx",
        OpenCoverReportsPath = "**/*.opencover.xml",
        Verbose = true,
        Version = packageVersion,
        ArgumentCustomization = args => {
            var a = args;

            if(!string.IsNullOrEmpty(sonarBranch))
            {
                a = a.Append($"/d:sonar.pullrequest.key=\"{sonarBranch}\"");
            }

            if(!string.IsNullOrEmpty(sonarBranchTitle))
            {
                a = a.Append($"/d:sonar.pullrequest.branch=\"{sonarBranchTitle}\"");
            }

            return a;
        }
    });
});

Task("SonarEnd")
    .Does(() =>
{
    SonarEnd(new SonarEndSettings{
        Login = sonarLogin,
     });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Tests");

Task("Sonar")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Tests")
    .IsDependentOn("SonarEnd");

Task("Release")
    .IsDependentOn("Sonar")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
