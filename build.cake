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
var sonarVersion = Argument("sonarVersion", default(string));

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
    if(string.IsNullOrEmpty(sonarVersion))
    {
        sonarVersion = EnvironmentVariable("SONAR_VERSION");
    }

    if(string.IsNullOrEmpty(sonarPrKey))
    {
        sonarPrKey = EnvironmentVariable("PR_NUMBER");
        sonarBranch = EnvironmentVariable("PR_SOURCE_BRANCH");
        sonarBranchBase = EnvironmentVariable("PR_TARGET_BRANCH");
        sonarBranchBase = "master";
        System.Console.WriteLine("PrKey" + sonarPrKey);
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

Task("Tests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/full_{i++}\" --blame")
    };

    DotNetCoreBuild("./tools/Build.Core.sln", buildSettings);

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Classic") && !file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("AllTests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = true,
        NoBuild = true,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/full_{i++}\" --blame")
    };

    DotNetCoreBuild("./tools/Build.sln", buildSettings);

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        DotNetCoreTest(file.FullPath, testSettings);
    }
});

Task("SonarBegin")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    SonarBegin(new SonarBeginSettings
    {
        UseCoreClr = true,
        Url = "https://sonarcloud.io",
        Login = sonarLogin,
        Key = "HotChocolate",
        Organization = "chillicream",
        VsTestReportsPath = "**/*.trx",
        OpenCoverReportsPath = "**/*.opencover.xml",
        Exclusions = "**/*.js,**/*.html,**/*.css,**/examples/**/*.*,**/StarWars/**/*.*,**/benchmarks/**/*.*,**/src/Templates/**/*.*",
        Verbose = false,
        Version = sonarVersion,
        ArgumentCustomization = a => {
            if(!string.IsNullOrEmpty(sonarPrKey))
            {
                a = a.Append($"/d:sonar.pullrequest.key=\"{sonarPrKey}\"");
                a = a.Append($"/d:sonar.pullrequest.branch=\"{sonarBranch}\"");
                a = a.Append($"/d:sonar.pullrequest.base=\"{sonarBranchBase}\"");
                a = a.Append($"/d:sonar.pullrequest.provider=\"github\"");
                a = a.Append($"/d:sonar.pullrequest.github.repository=\"ChilliCream/hotchocolate\"");
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
        UseCoreClr = true,
        Login = sonarLogin
    });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Sonar");

Task("Sonar")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("AllTests")
    .IsDependentOn("SonarEnd");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
