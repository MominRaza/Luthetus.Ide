using Fluxor;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;
using Luthetus.Ide.RazorLib.FolderExplorers.States;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.Commands.Models;
using Luthetus.Common.RazorLib.Menus.Models;
using Luthetus.Common.RazorLib.Dropdowns.Models;
using Luthetus.Common.RazorLib.TreeViews.Models;
using Luthetus.Common.RazorLib.Notifications.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.Dimensions.Models;
using Luthetus.Ide.RazorLib.Menus.Models;
using Luthetus.Ide.RazorLib.FileSystems.Models;

namespace Luthetus.Ide.RazorLib.FolderExplorers.Displays;

public partial class FolderExplorerContextMenu : ComponentBase
{
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
    [Inject]
    private IMenuOptionsFactory MenuOptionsFactory { get; set; } = null!;
    [Inject]
    private ILuthetusCommonComponentRenderers LuthetusCommonComponentRenderers { get; set; } = null!;
    [Inject]
    private ITreeViewService TreeViewService { get; set; } = null!;

    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; } = null!;

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    /// <summary>
    /// The program is currently running using Photino locally on the user's computer
    /// therefore this static solution works without leaking any information.
    /// </summary>
    public static TreeViewNoType? ParentOfCutFile;

    private MenuRecord GetMenuRecord(TreeViewCommandArgs treeViewCommandArgs)
    {
        if (treeViewCommandArgs.NodeThatReceivedMouseEvent is null)
            return MenuRecord.Empty;

        var menuRecordsList = new List<MenuOptionRecord>();

        var treeViewModel = treeViewCommandArgs.NodeThatReceivedMouseEvent;
        var parentTreeViewModel = treeViewModel.Parent;

        var parentTreeViewAbsolutePath = parentTreeViewModel as TreeViewAbsolutePath;

        if (treeViewModel is not TreeViewAbsolutePath treeViewAbsolutePath)
            return MenuRecord.Empty;

        if (treeViewAbsolutePath.Item.IsDirectory)
        {
            menuRecordsList.AddRange(GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                .Union(GetDirectoryMenuOptions(treeViewAbsolutePath))
                .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }
        else
        {
            menuRecordsList.AddRange(GetFileMenuOptions(treeViewAbsolutePath, parentTreeViewAbsolutePath)
                .Union(GetDebugMenuOptions(treeViewAbsolutePath)));
        }

        return new MenuRecord(menuRecordsList.ToImmutableArray());
    }

    private MenuOptionRecord[] GetDirectoryMenuOptions(TreeViewAbsolutePath treeViewModel)
    {
        return new[]
        {
            MenuOptionsFactory.NewEmptyFile(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.NewDirectory(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.PasteClipboard(
                treeViewModel.Item,
                async () => 
                {
                    var localParentOfCutFile = ParentOfCutFile;
                    ParentOfCutFile = null;

                    if (localParentOfCutFile is not null)
                        await ReloadTreeViewModel(localParentOfCutFile).ConfigureAwait(false);

                    await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false);
                }),
        };
    }

    private MenuOptionRecord[] GetFileMenuOptions(
        TreeViewAbsolutePath treeViewModel,
        TreeViewAbsolutePath? parentTreeViewModel)
    {
        return new[]
        {
            MenuOptionsFactory.CopyFile(
                treeViewModel.Item,
                () => {
                    NotificationHelper.DispatchInformative("Copy Action", $"Copied: {treeViewModel.Item.NameWithExtension}", LuthetusCommonComponentRenderers, Dispatcher, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                }),
            MenuOptionsFactory.CutFile(
                treeViewModel.Item,
                () => {
                    NotificationHelper.DispatchInformative("Cut Action", $"Cut: {treeViewModel.Item.NameWithExtension}", LuthetusCommonComponentRenderers, Dispatcher, TimeSpan.FromSeconds(7));
                    ParentOfCutFile = parentTreeViewModel;
                    return Task.CompletedTask;
                }),
            MenuOptionsFactory.DeleteFile(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.RenameFile(
                treeViewModel.Item,
                Dispatcher,
                async ()  => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false))
        };
    }

    private MenuOptionRecord[] GetDebugMenuOptions(TreeViewAbsolutePath treeViewModel)
    {
        return new MenuOptionRecord[]
        {
            // new MenuOptionRecord(
            //     $"namespace: {treeViewModel.Item.Namespace}",
            //     MenuOptionKind.Read)
        };
    }

    /// <summary>
    /// This method I believe is causing bugs
    /// <br/><br/>
    /// For example, when removing a C# Project the
    /// solution is reloaded and a new root is made.
    /// <br/><br/>
    /// Then there is a timing issue where the new root is made and set
    /// as the root. But this method erroneously reloads the old root.
    /// </summary>
    /// <param name="treeViewModel"></param>
    private async Task ReloadTreeViewModel(TreeViewNoType? treeViewModel)
    {
        if (treeViewModel is null)
            return;

        await treeViewModel.LoadChildListAsync().ConfigureAwait(false);

        TreeViewService.ReRenderNode(
            FolderExplorerState.TreeViewContentStateKey,
            treeViewModel);

        TreeViewService.MoveUp(
            FolderExplorerState.TreeViewContentStateKey,
            false,
			false);
    }

    public static string GetContextMenuCssStyleString(TreeViewCommandArgs? treeViewCommandArgs)
    {
        if (treeViewCommandArgs?.ContextMenuFixedPosition is null)
            return "display: none;";

        var left =
            $"left: {treeViewCommandArgs.ContextMenuFixedPosition.LeftPositionInPixels.ToCssValue()}px;";

        var top =
            $"top: {treeViewCommandArgs.ContextMenuFixedPosition.TopPositionInPixels.ToCssValue()}px;";

        return $"{left} {top}";
    }
}