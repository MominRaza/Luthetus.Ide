using Fluxor;
using Luthetus.Ide.RazorLib.ComponentRenderers.Models;
using Luthetus.Ide.RazorLib.Gits.Models;
using Luthetus.Ide.RazorLib.Gits.States;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.Gits.Displays;

public partial class TreeViewGitFileDisplay : ComponentBase, ITreeViewGitFileRendererType
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;

    [CascadingParameter]
    public GitState GitState { get; set; } = null!;

    [Parameter, EditorRequired]
    public TreeViewGitFile TreeViewGitFile { get; set; } = null!;

    private bool IsChecked
    {
        get => GitState.SelectedFileList.Any(x => x.AbsolutePath.Value == TreeViewGitFile.Item.AbsolutePath.Value);
        set
        {
            var localGitState = GitState;

            Dispatcher.Dispatch(new GitState.WithAction(inState =>
            {
                if (inState.Repo != localGitState.Repo)
                {
                    // Git folder was changed, throw away the result since it is thereby invalid.
                    return inState;
                }

                var key = TreeViewGitFile.Item.AbsolutePath.Value;
                ImmutableList<GitFile> outSelectedFileList;

                var indexOf = inState.SelectedFileList.FindIndex(x => x.AbsolutePath.Value == key);

                // Toggle
                if (indexOf != -1)
                    outSelectedFileList = inState.SelectedFileList.RemoveAt(indexOf);
                else
                    outSelectedFileList = inState.SelectedFileList.Add(TreeViewGitFile.Item);

                return inState with
                {
                    SelectedFileList = outSelectedFileList
                };
            }));
        }
    }
}