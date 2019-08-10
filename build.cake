#addin "nuget:?package=Cake.Sonar&version=1.1.18"
#addin "nuget:?package=Cake.FileHelpers&version=3.1.0"
#addin "nuget:?package=Cake.NuGet&version=0.30.0"
#tool "nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.3.1"


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
    DotNetCoreClean("./src/DataLoader");
    DotNetCoreClean("./src/Core");
    DotNetCoreClean("./src/Server");
});

Task("Restore")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    DotNetCoreRestore("./tools/Build.sln");
});

Task("RestoreCore")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    DotNetCoreRestore("./tools/Build.Core.sln");
});

Task("Build")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
    };

    DotNetCoreBuild("./tools/Build.sln", settings);
});

Task("BuildDebug")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    DotNetCoreBuild("./tools/Build.sln", buildSettings);
});

Task("BuildCore")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = configuration,
    };

    DotNetCoreBuild("./tools/Build.Core.sln", settings);
});

Task("Publish")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "./tools/Build.sln /t:restore /p:configuration=" + configuration }))
    {
        process.WaitForExit();
    }

    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "./tools/Build.sln /t:build /p:configuration=" + configuration }))
    {
        process.WaitForExit();
    }

    using(var process = StartAndReturnProcess("msbuild",
        new ProcessSettings{ Arguments = "./tools/Build.sln /t:pack /p:configuration=" + configuration + " /p:IncludeSource=true /p:IncludeSymbols=true" }))
    {
        process.WaitForExit();
    }
});

Task("PublishTemplates")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var nuGetPackSettings   = new NuGetPackSettings
    {
        Version = packageVersion,
        OutputDirectory = "src/Templates"
    };

    ReplaceTextInFiles("src/Templates/StarWars/content/StarWars/StarWars.csproj", "9.0.4", packageVersion);
    ReplaceTextInFiles("src/Templates/Server/content/HotChocolate.Server.csproj", "9.0.4", packageVersion);
    NuGetPack("src/Templates/StarWars/HotChocolate.Templates.StarWars.nuspec", nuGetPackSettings);
    NuGetPack("src/Templates/Server/HotChocolate.Templates.Server.nuspec", nuGetPackSettings);
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

    DotNetCoreBuild("./tools/Build.sln", buildSettings);

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("TemplatesCompile")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    var buildSettings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug"
    };

    DotNetCoreBuild("./src/Templates/Server/content", buildSettings);
    DotNetCoreBuild("./src/Templates/StarWars/content", buildSettings);
});

Task("CoreTests")
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
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/core_{i++}\" --blame")
    };

    DotNetCoreBuild("./tools/Build.Core.sln", buildSettings);

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("RedisTests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/core_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        if(file.FullPath.Contains("Redis"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("MongoTests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/core_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/**/*.Tests.csproj"))
    {
        if(file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("HC_DataLoader_Tests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/hc_dataloader_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/DataLoader/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});


Task("HC_Core_Tests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/hc_core_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/Core/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("HC_Server_Tests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/hc_server_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/Server/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
});

Task("HC_Stitching_Tests")
    .IsDependentOn("EnvironmentSetup")
    .Does(() =>
{
    int i = 0;
    var testSettings = new DotNetCoreTestSettings
    {
        Configuration = "Debug",
        ResultsDirectory = $"./{testOutputDir}",
        Logger = "trx",
        NoRestore = false,
        NoBuild = false,
        ArgumentCustomization = args => args
            .Append("/p:CollectCoverage=true")
            .Append("/p:Exclude=[xunit.*]*")
            .Append("/p:CoverletOutputFormat=opencover")
            .Append($"/p:CoverletOutput=\"../../{testOutputDir}/hc_stitching_{i++}\" --blame")
    };

    foreach(var file in GetFiles("./src/Stitching/**/*.Tests.csproj"))
    {
        if(!file.FullPath.Contains("Redis") && !file.FullPath.Contains("Mongo"))
        {
            DotNetCoreTest(file.FullPath, testSettings);
        }
    }
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
    .IsDependentOn("Tests");

Task("Sonar")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("Tests")
    .IsDependentOn("SonarEnd");

Task("SonarSlim")
    .IsDependentOn("SonarBegin")
    .IsDependentOn("BuildDebug")
    .IsDependentOn("SonarEnd");

Task("Release")
    .IsDependentOn("Sonar")
    .IsDependentOn("Publish")
    .IsDependentOn("PublishTemplates");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
