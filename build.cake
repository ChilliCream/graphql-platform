#addin "nuget:?package=Cake.Sonar&version=1.1.22"
#addin "nuget:?package=Cake.FileHelpers&version=3.2.1"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.6.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var sonarLogin = Argument("sonarLogin", default(string));
var sonarPrKey = Argument("sonarPrKey", default(string));
var sonarBranch = Argument("sonarBranch", default(string));
var sonarBranchBase = Argument("sonarBranch", default(string));
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
            ?? EnvironmentVariable("APPVEYOR_REPO_TAG_NAME")
            ?? EnvironmentVariable("Version");
    }
    Environment.SetEnvironmentVariable("Version", packageVersion);

    if(string.IsNullOrEmpty(sonarPrKey))
    {
        sonarPrKey = EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER");
        sonarBranch = EnvironmentVariable("APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH");
        sonarBranchBase = EnvironmentVariable("APPVEYOR_REPO_BRANCH");
        sonarBranchBase = "master";
    }

    if(string.IsNullOrEmpty(sonarLogin))
    {
        sonarLogin = EnvironmentVariable("SONAR_TOKEN");
    }
});

Task("Build")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    DotNetCoreBuild("./tools/Build.sln", buildSettings);
});

Task("SonarBegin")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    SonarBegin(new SonarBeginSettings
    {
        Url = "https://sonarcloud.io",
        Login = sonarLogin,
        Key = "HotChocolate",
        Organization = "chillicream",
        VsTestReportsPath = "**/*.trx",
        OpenCoverReportsPath = "**/*.opencover.xml",
        Exclusions = "**/*.js,**/*.html,**/*.css,**/examples/**/*.*,**/StarWars/**/*.*,**/benchmarks/**/*.*,**/src/Templates/**/*.*",
        Verbose = false,
        Version = packageVersion,
        ArgumentCustomization = a => {
            if(!string.IsNullOrEmpty(sonarPrKey))
            {
                a = a.Append($"/d:sonar.pullrequest.key=\"{sonarPrKey}\"");
                a = a.Append($"/d:sonar.pullrequest.branch=\"{sonarBranch}\"");
                a = a.Append($"/d:sonar.pullrequest.base=\"{sonarBranchBase}\"");
                a = a.Append($"/d:sonar.pullrequest.provider=\"github\"");
                a = a.Append($"/d:sonar.pullrequest.github.repository=\"ChilliCream/hotchocolate\"");
                // a = a.Append($"/d:sonar.pullrequest.github.endpoint=\"https://api.github.com/\"");
            }
            return a;
        }
    });
});

Task("SonarEnd")
    .Does(() =>
{
    SonarEnd(new SonarEndSettings
    {
        Login = sonarLogin,
    });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Sonar");

Task("Sonar")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Build")
    .IsDependentOn("SonarEnd");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
