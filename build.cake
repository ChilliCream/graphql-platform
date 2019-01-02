#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#addin "nuget:?package=Cake.Sonar"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool"

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

Task("Clean")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    DotNetCoreClean("./src/Core");
    DotNetCoreClean("./src/Server");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Core /t:restore /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }

    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Server /t:restore /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Core /t:build /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }

    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Server /t:build /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }
});

Task("Publish")
    .IsDependentOn("Build")
    .Does(() =>
{
    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Core /t:pack /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }

    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "src/Server /t:pack /p:configuration=" + configuration}))
    {
        process.WaitForExit();
    }
});

Task("Tests")
    .IsDependentOn("CoreTests")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug",
        NoRestore = false,
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
            .Append($"/p:CollectCoverage=true")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/classic_{i++}\" --blame")
    };

    DotNetCoreBuild("./src/Server/AspNetClassic.Tests", buildSettings);
    DotNetCoreTest("./src/Server/AspNetClassic.Tests", testSettings);
});

Task("CoreTests")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug",
        NoRestore = false,
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
            .Append($"/p:CollectCoverage=true")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/core_{i++}\" --blame")
    };

    DotNetCoreBuild("./src/Core", buildSettings);
    DotNetCoreBuild("./src/Server/AspNetCore.Tests", buildSettings);

    DotNetCoreTest("./src/Core/Utilities.Tests", testSettings);
    DotNetCoreTest("./src/Core/Abstractions.Tests", testSettings);
    DotNetCoreTest("./src/Core/Runtime.Tests", testSettings);
    DotNetCoreTest("./src/Core/Language.Tests", testSettings);
    DotNetCoreTest("./src/Core/Types.Tests", testSettings);
    DotNetCoreTest("./src/Core/Validation.Tests", testSettings);
    DotNetCoreTest("./src/Core/Core.Tests", testSettings);
    DotNetCoreTest("./src/Core/Subscriptions.Tests", testSettings);
    DotNetCoreTest("./src/Core/Stitching.Tests", testSettings);
    DotNetCoreTest("./src/Server/AspNetCore.Tests", testSettings);
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
        Exclusions = "**/*.js,**/*.html,**/*.css,**/src/Core/Benchmark.Tests/**/*.*,**/src/Templates/**/*.*",
        // Verbose = true,
        Version = packageVersion,
        ArgumentCustomization = args => {
            var a = args;

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
