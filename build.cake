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


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
// Define directories.
var testOutputDir = Directory("./testoutput");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("EnvironmentSetup")
    .Does(() =>
{
    string version = EnvironmentVariable("APPVEYOR_REPO_TAG_NAME");
    if(string.IsNullOrEmpty(version))
    {
        version = EnvironmentVariable("APPVEYOR_BUILD_VERSION");
    }

    if(string.IsNullOrEmpty(version))
    {
        version = EnvironmentVariable("Version");
    }

    if(string.IsNullOrEmpty(sonarBranch))
    {
        sonarBranch = EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER");
        sonarBranchTitle = EnvironmentVariable("APPVEYOR_PULL_REQUEST_TITLE");
    }

    Environment.SetEnvironmentVariable("Version", version);
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
    };
    DotNetCoreBuild("./src", settings);
});

Task("Tests")
    .IsDependentOn("Restore")
    .Does(() =>
{
    int i = 0;
    var settings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ArgumentCustomization = args => args.Append($"/p:CollectCoverage=true")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/{i++}\""),
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = true
    };

    DotNetCoreTest("./src/Language.Tests", settings);
    DotNetCoreTest("./src/Runtime.Tests", settings);
    DotNetCoreTest("./src/Core.Tests", settings);
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
        Version = EnvironmentVariable("Version"),
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
  .IsDependentOn("Build")
  .IsDependentOn("Tests")
  .IsDependentOn("SonarEnd");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
