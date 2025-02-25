using Luthetus.Common.RazorLib.FileSystems.Models;
using Luthetus.Ide.RazorLib.BackgroundTasks.Models;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.Shareds.Displays.Internals;

public partial class IdePromptOpenSolutionDisplay : ComponentBase
{
    [Inject]
    private LuthetusIdeBackgroundTaskApi IdeBackgroundTaskApi { get; set; } = null!;

    [Parameter, EditorRequired]
    public IAbsolutePath AbsolutePath { get; set; } = null!;

    private async Task OpenSolutionOnClick()
    {
        await IdeBackgroundTaskApi.DotNetSolution
            .SetDotNetSolution(AbsolutePath)
            .ConfigureAwait(false);
    }
}