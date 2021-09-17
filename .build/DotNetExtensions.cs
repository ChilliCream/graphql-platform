using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Helpers;


public static class DotNetExtensions
{
    public static IEnumerable<DotNetTestSettings> Foo(DotNetTestSettings settings,
        IEnumerable<Project> projects)
    {
        IEnumerable<DotNetTestSettings> TestSettings(
            DotNetTestSettings settings,
            IEnumerable<Project> projects) =>
            settings
                .SetConfiguration("Debug")
                .SetNoRestore(true)
                .SetNoBuild(true)
                .ResetVerbosity()
                .SetResultsDirectory(TestResultDirectory);
                .CombineWith(TestProjects, (_, v) => _
                    .SetProjectFile(v)
                    .SetLoggers($"trx;LogFileName={v.Name}.trx"));
    }
}
