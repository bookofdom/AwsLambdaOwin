#tool "nuget:?package=xunit.runner.console&version=2.1.0"
#addin "Cake.FileHelpers"

var target          = Argument("target", "Default");
var configuration   = Argument("configuration", "Release");
var artifactsDir    = Directory("./artifacts");
var solution        = "./src/AwsLambdaOwin.sln";
var buildNumber     = string.IsNullOrWhiteSpace(EnvironmentVariable("APPVEYOR_BUILD_NUMBER")) 
                        ? "0" 
                        : EnvironmentVariable("APPVEYOR_BUILD_NUMBER");
var version         = FileReadText("version.txt");
var commitSha       = string.IsNullOrWhiteSpace(EnvironmentVariable("APPVEYOR_REPO_COMMIT"))
                        ? ""
                        : EnvironmentVariable("APPVEYOR_REPO_COMMIT");

Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
});

Task("RestorePackages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(solution);
    DotNetCoreRestore(solution);
});

Task("Build")
    .IsDependentOn("RestorePackages")
    .Does(() =>
{
    var settings = new DotNetCoreBuildSettings
    {
        ArgumentCustomization = args => 
            args.Append("/p:Version=" + version + ";FileVersion=" + version + ";InformationalVersion=" + commitSha),
        Configuration = configuration
    };

    DotNetCoreBuild(solution, settings);
});

Task("RunTests")
    .IsDependentOn("Build")
    .Does(() =>
{
        var dll = "./src/AwsLambdaOwin.Tests/bin/" + configuration + "/AwsLambdaOwin.Tests.dll";
        var settings =  new XUnitSettings 
        { 
            ToolPath = "./tools/xunit.runner.console/tools/xunit.console.exe"
        };
        XUnit(dll, settings);
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    var packageVersion = "ci" + buildNumber.PadLeft(5, '0');

    var settings = new DotNetCorePackSettings
    {
        /*ArgumentCustomization = args => args
            .Append("--include-symbols")
            .Append("--include-source"),*/
        Configuration = "Release",
        OutputDirectory = "./artifacts/",
        VersionSuffix = packageVersion,
        NoBuild = true,
    };
    DotNetCorePack("./src/AwsLambdaOwin", settings);
});

Task("Default")
    .IsDependentOn("RunTests")
    .IsDependentOn("Pack");

RunTarget(target);