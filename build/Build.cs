using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using GCore.Extensions.ArrayEx;
using GCore.Extensions.StringShEx;
using GCore.Logging;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main()
    {
        Log.LoggingHandler.Add(new GCore.Logging.Logger.ConsoleLogger());
        return Execute<Build>(x => x.Compile);
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath ReadmeFile => RootDirectory / "README.md";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            var version = new Version(1, 0, 0);
            try {
                version = "git tag".Sh().Split('\n').Get(-2).ExtractVersion();
            } catch {}

            var commitIndex = "11";// "git rev-list --count HEAD".Sh().Replace("\n", "").Trim();

            Log.Info("Repo version:" + version + "." + commitIndex);

            foreach (var project in Solution.AllProjects.Where(p => p.Name == "SharpEXR"))
                DotNetPack(_ => _
                    .SetProject(project)
                    .SetNoBuild(InvokedTargets.Contains(Compile))
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetVersion(version + "." + commitIndex)
                    .SetPackageReleaseNotes(ReadmeFile)
                    );
        });

    [Parameter("NuGet Api Key")] readonly string ApiKey;
    [Parameter("NuGet Source for Packages")] readonly string Source = "https://api.nuget.org/v3/index.json";

    Target Publish => _ => _
        .DependsOn(Clean, Pack)
        .Consumes(Pack)
        .Requires(() => ApiKey)
        .Executes(() => {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");
            Debug.Assert(packages.Count == 1, "packages.Count == 4");

            DotNetNuGetPush(_ => _
                    .SetSource(Source)
                    .SetApiKey(ApiKey)
                    .CombineWith(packages, (_, v) => _
                        .SetTargetPath(v)),
                degreeOfParallelism: 5,
                completeOnFailure: true);
        });
}
