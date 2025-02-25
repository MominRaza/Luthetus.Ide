using Luthetus.Ide.RazorLib.BackgroundTasks.Models;
using Luthetus.Ide.RazorLib.Gits.States;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.Gits.Displays;

public partial class GitBranchCheckoutDisplay : ComponentBase
{
    [Inject]
    private LuthetusIdeBackgroundTaskApi IdeBackgroundTaskApi { get; set; } = null!;

    [Parameter, EditorRequired]
    public GitState GitState { get; set; } = null!;

    private async Task SetActiveBranchOnClick(GitState localGitState, string branchName)
    {
        if (localGitState.Repo is null)
            return;

        await IdeBackgroundTaskApi.Git.BranchSetEnqueue(localGitState.Repo, branchName);
    }
}