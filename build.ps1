param([switch]$DisableBuild, [switch]$RunTests, [switch]$EnableCoverage, [switch]$EnableSonar, [switch]$Pack, [switch]$PR)

$testResults = Join-Path -Path $PSScriptRoot -ChildPath ".testresults"

if (!!$env:APPVEYOR_REPO_TAG_NAME) {
    $version = $env:APPVEYOR_REPO_TAG_NAME
}
elseif (!!$env:APPVEYOR_BUILD_VERSION) {
    $version = $env:APPVEYOR_BUILD_VERSION
}

$prKey = $env:APPVEYOR_PULL_REQUEST_NUMBER
$prName = $env:APPVEYOR_PULL_REQUEST_TITLE
$sonarLogin = $env:SONAR_TOKEN

if ($PR) {
    Write-Host "PR Key: " $prKey
    Write-Host "PR Name: " $prName
}

if ($null -ne $version) {
    $env:Version = $version
}

if ($env:Version.Contains("-")) {
    $index = $env:Version.IndexOf("-");
    $env:VersionPrefix = $env:Version.Substring(0, $index)
    $env:VersionSuffix = $env:Version.Substring($index + 1)
    $env:PreVersion = $true
}
else {
    $env:VersionPrefix = $env:Version
    $env:VersionSuffix = $null
    $env:PreVersion = $false
}

if ($EnableSonar) {
    dotnet tool install --global dotnet-sonarscanner

    if ($PR) {
      dotnet sonarscanner begin /k:"HotChocolate" /d:sonar.organization="chillicream" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="$sonarLogin" /d:sonar.cs.vstest.reportsPaths="$testResults\*.trx" /d:sonar.cs.opencover.reportsPaths="$PSScriptRoot\opencover.xml" /d:sonar.pullrequest.branch="$prName" /d:sonar.pullrequest.key="$prKey"
    }
    else {
      dotnet sonarscanner begin /k:"HotChocolate" /d:sonar.organization="chillicream" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="$sonarLogin" /v:"$env:VersionPrefix" /d:sonar.cs.vstest.reportsPaths="$testResults\*.trx" /d:sonar.cs.opencover.reportsPaths="$PSScriptRoot\opencover.xml"
    }
}

if ($DisableBuild -eq $false) {
    dotnet build src
}

if ($RunTests -or $EnableCoverage) {
    # Test
    $serachDirs = [System.IO.Path]::Combine($PSScriptRoot, "src", "*", "bin", "Debug", "netcoreapp2.0")
    $runTestsCmd = [System.Guid]::NewGuid().ToString("N") + ".cmd"
    $runTestsCmd = Join-Path -Path $env:TEMP -ChildPath $runTestsCmd
    $testAssemblies = ""

    Get-ChildItem src -Directory -Filter *.Tests | Where-Object {$_.Name.StartsWith("Benchmark") -eq $false} | % { $testAssemblies += "dotnet test `"" + $_.FullName + "`" -r `"" + $testResults + "`" -l trx`n" }

    if (!!$testAssemblies) {
        # Has test assemblies {
        $userDirectory = $env:USERPROFILE
        if ($IsMacOS) {
            $userDirectory = $env:HOME
        }

        [System.IO.File]::WriteAllText($runTestsCmd, $testAssemblies)
        Write-Host $runTestsCmd

        if ($EnableCoverage) {
            # Test & Code Coverage
            $nugetPackages = [System.IO.Path]::Combine($userDirectory, ".nuget", "packages")

            $openCover = [System.IO.Path]::Combine($nugetPackages, "OpenCover", "*", "tools", "OpenCover.Console.exe")
            $openCover = Resolve-Path $openCover

            $coveralls = [System.IO.Path]::Combine($nugetPackages, "coveralls.io", "*", "tools", "coveralls.net.exe")
            $coveralls = Resolve-Path $coveralls

            & $openCover -register:user -target:"$runTestsCmd" -searchdirs:"$serachDirs" -oldstyle -output:coverage.xml -skipautoprops -returntargetcode -filter:"+[HotChocolate*]*"
            if($PR -eq $false) {
              & $coveralls --opencover coverage.xml
            }
        }
        else {
            # Test
            & $runTestsCmd
        }
    }
}

if ($EnableSonar) {
    dotnet sonarscanner end /d:sonar.login="$sonarLogin"
}

if ($Pack) {
    $dropRootDirectory = Join-Path -Path $PSScriptRoot -ChildPath "drop"

    if ($env:PreVersion) {
        dotnet pack ./src -c Release -o $dropRootDirectory /p:PackageVersion=$env:Version /p:VersionPrefix=$env:VersionPrefix /p:VersionSuffix=$env:VersionSuffix --include-source --include-symbols
    }
    else {
        dotnet pack ./src -c Release -o $dropRootDirectory /p:PackageVersion=$env:Version /p:VersionPrefix=$env:VersionPrefix --include-source --include-symbols
    }
}

