using Fluxor;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;
using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.Namespaces.Models;
using Luthetus.Common.RazorLib.Dialogs.States;
using Luthetus.Common.RazorLib.Commands.Models;
using Luthetus.Common.RazorLib.Dialogs.Models;
using Luthetus.Common.RazorLib.TreeViews.Models;
using Luthetus.Common.RazorLib.Menus.Models;
using Luthetus.Common.RazorLib.Dropdowns.Models;
using Luthetus.Common.RazorLib.Notifications.Models;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Common.RazorLib.Dimensions.Models;
using Luthetus.Common.RazorLib.FileSystems.Models;
using Luthetus.Common.RazorLib.BackgroundTasks.Models;
using Luthetus.TextEditor.RazorLib.Lexes.Models;
using Luthetus.TextEditor.RazorLib.TextEditors.Models;
using Luthetus.CompilerServices.Lang.DotNetSolution.Models;
using Luthetus.Ide.RazorLib.DotNetSolutions.States;
using Luthetus.Ide.RazorLib.Terminals.States;
using Luthetus.Ide.RazorLib.ProgramExecutions.States;
using Luthetus.Ide.RazorLib.InputFiles.Models;
using Luthetus.Ide.RazorLib.Menus.Models;
using Luthetus.Ide.RazorLib.CommandLines.Models;
using Luthetus.Ide.RazorLib.Terminals.Models;
using Luthetus.Ide.RazorLib.ComponentRenderers.Models;
using Luthetus.Ide.RazorLib.FormsGenerics.Displays;
using Luthetus.Common.RazorLib.Dynamics.Models;
using Luthetus.Ide.RazorLib.BackgroundTasks.Models;
using Luthetus.TextEditor.RazorLib.Groups.Models;
using Luthetus.TextEditor.RazorLib.Installations.Models;
using Luthetus.Ide.RazorLib.DotNetSolutions.Models;
using Luthetus.Ide.RazorLib.CSharpProjects.Models;
using Luthetus.Ide.RazorLib.CSharpProjects.Displays;
using Luthetus.Ide.RazorLib.Namespaces.Models;

namespace Luthetus.Ide.RazorLib.DotNetSolutions.Displays;

public partial class SolutionExplorerContextMenu : ComponentBase
{
    [Inject]
    private IState<TerminalState> TerminalStateWrap { get; set; } = null!;
    [Inject]
    private IDispatcher Dispatcher { get; set; } = null!;
    [Inject]
    private IMenuOptionsFactory MenuOptionsFactory { get; set; } = null!;
    [Inject]
    private ILuthetusCommonComponentRenderers CommonComponentRenderers { get; set; } = null!;
    [Inject]
    private ILuthetusIdeComponentRenderers IdeComponentRenderers { get; set; } = null!;
    [Inject]
    private ITreeViewService TreeViewService { get; set; } = null!;
    [Inject]
    private LuthetusIdeBackgroundTaskApi IdeBackgroundTaskApi { get; set; } = null!;
    [Inject]
    private IEnvironmentProvider EnvironmentProvider { get; set; } = null!;
    [Inject]
    private IFileSystemProvider FileSystemProvider { get; set; } = null!;
    [Inject]
    private IBackgroundTaskService BackgroundTaskService { get; set; } = null!;
    [Inject]
    private LuthetusTextEditorConfig TextEditorConfig { get; set; } = null!;
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    [Parameter, EditorRequired]
    public TreeViewCommandArgs TreeViewCommandArgs { get; set; } = null!;

	private static readonly Key<IDynamicViewModel> _solutionEditorDialogKey = Key<IDynamicViewModel>.NewKey();
	private static readonly Key<IDynamicViewModel> _newCSharpProjectDialogKey = Key<IDynamicViewModel>.NewKey();

    public static readonly Key<DropdownRecord> ContextMenuEventDropdownKey = Key<DropdownRecord>.NewKey();

    /// <summary>
    /// The program is currently running using Photino locally on the user's computer
    /// therefore this static solution works without leaking any information.
    /// </summary>
    public static TreeViewNoType? ParentOfCutFile;

    private MenuRecord GetMenuRecord(TreeViewCommandArgs commandArgs)
    {
        if (commandArgs.TreeViewContainer.SelectedNodeList.Count > 1)
            return GetMenuRecordManySelections(commandArgs);

        if (commandArgs.TreeViewContainer.ActiveNode is null)
            return MenuRecord.Empty;

        var menuOptionList = new List<MenuOptionRecord>();
        var treeViewModel = commandArgs.TreeViewContainer.ActiveNode;
        var parentTreeViewModel = treeViewModel.Parent;
        var parentTreeViewNamespacePath = parentTreeViewModel as TreeViewNamespacePath;

        if (treeViewModel is TreeViewNamespacePath treeViewNamespacePath)
        {
            if (treeViewNamespacePath.Item.AbsolutePath.IsDirectory)
            {
                menuOptionList.AddRange(GetFileMenuOptions(treeViewNamespacePath, parentTreeViewNamespacePath)
                    .Union(GetDirectoryMenuOptions(treeViewNamespacePath))
                    .Union(GetDebugMenuOptions(treeViewNamespacePath)));
            }
            else
            {
                switch (treeViewNamespacePath.Item.AbsolutePath.ExtensionNoPeriod)
                {
                    case ExtensionNoPeriodFacts.C_SHARP_PROJECT:
                        menuOptionList.AddRange(GetCSharpProjectMenuOptions(treeViewNamespacePath)
                            .Union(GetDebugMenuOptions(treeViewNamespacePath)));
                        break;
                    default:
                        menuOptionList.AddRange(GetFileMenuOptions(treeViewNamespacePath, parentTreeViewNamespacePath)
                            .Union(GetDebugMenuOptions(treeViewNamespacePath)));
                        break;
                }
            }
        }
        else if (treeViewModel is TreeViewSolution treeViewSolution)
        {
            if (ExtensionNoPeriodFacts.DOT_NET_SOLUTION == treeViewSolution.Item.NamespacePath.AbsolutePath.ExtensionNoPeriod)
            {
                if (treeViewSolution.Parent is null || treeViewSolution.Parent is TreeViewAdhoc)
                    menuOptionList.AddRange(GetDotNetSolutionMenuOptions(treeViewSolution));
            }
        }
        else if (treeViewModel is TreeViewCSharpProjectToProjectReference treeViewCSharpProjectToProjectReference)
        {
            menuOptionList.AddRange(GetCSharpProjectToProjectReferenceMenuOptions(
                treeViewCSharpProjectToProjectReference));
        }
        else if (treeViewModel is TreeViewCSharpProjectNugetPackageReference treeViewCSharpProjectNugetPackageReference)
        {
            menuOptionList.AddRange(GetTreeViewLightWeightNugetPackageRecordMenuOptions(
                treeViewCSharpProjectNugetPackageReference));
        }

        if (!menuOptionList.Any())
            return MenuRecord.Empty;

        return new MenuRecord(menuOptionList.ToImmutableArray());
    }

    private MenuRecord GetMenuRecordManySelections(TreeViewCommandArgs commandArgs)
    {
        var menuOptionList = new List<MenuOptionRecord>();

        var getFileOptions = true;
        var filenameList = new List<string>();

        foreach (var selectedNode in commandArgs.TreeViewContainer.SelectedNodeList)
        {
            if (selectedNode is TreeViewNamespacePath treeViewNamespacePath)
            {
                if (treeViewNamespacePath.Item.AbsolutePath.ExtensionNoPeriod == ExtensionNoPeriodFacts.C_SHARP_PROJECT)
                    getFileOptions = false;
                else if (getFileOptions)
                    filenameList.Add(treeViewNamespacePath.Item.AbsolutePath.NameWithExtension + " __FROM__ " + (treeViewNamespacePath.Item.AbsolutePath.ParentDirectory?.Value ?? "null"));
            }
            else
            {
                getFileOptions = false;
            }
        }

        if (getFileOptions)
        {
            menuOptionList.Add(new MenuOptionRecord(
                "Delete",
                MenuOptionKind.Delete,
                WidgetRendererType: IdeComponentRenderers.BooleanPromptOrCancelRendererType,
                WidgetParameterMap: new Dictionary<string, object?>
                {
                    { nameof(IBooleanPromptOrCancelRendererType.IncludeCancelOption), false },
                    { nameof(IBooleanPromptOrCancelRendererType.Message), $"DELETE:" },
                    { nameof(BooleanPromptOrCancelDisplay.ListOfMessages), filenameList },
                    { nameof(IBooleanPromptOrCancelRendererType.AcceptOptionTextOverride), null },
                    { nameof(IBooleanPromptOrCancelRendererType.DeclineOptionTextOverride), null },
                    { 
                        nameof(IBooleanPromptOrCancelRendererType.OnAfterAcceptFunc),
                        async () =>
                        {
                            await commandArgs.RestoreFocusToTreeView
                                .Invoke()
                                .ConfigureAwait(false);

                            await BackgroundTaskService.EnqueueAsync(
                                    Key<BackgroundTask>.NewKey(),
                                    ContinuousBackgroundTaskWorker.GetQueueKey(),
                                    "SolutionExplorer_TreeView_MultiSelect_DeleteFiles",
                                    async () =>
                                    {
                                        foreach (var node in commandArgs.TreeViewContainer.SelectedNodeList)
                                        {
                                            var treeViewNamespacePath = (TreeViewNamespacePath)node;

                                            if (treeViewNamespacePath.Item.AbsolutePath.IsDirectory)
                                            {
                                                await FileSystemProvider.Directory
                                                    .DeleteAsync(treeViewNamespacePath.Item.AbsolutePath.Value, true, CancellationToken.None)
                                                    .ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                await FileSystemProvider.File
                                                    .DeleteAsync(treeViewNamespacePath.Item.AbsolutePath.Value)
                                                    .ConfigureAwait(false);
                                            }

                                            if (TreeViewService.TryGetTreeViewContainer(commandArgs.TreeViewContainer.Key, out var mostRecentContainer) &&
                                                mostRecentContainer is not null)
                                            {
                                                var localParent = node.Parent;

                                                if (localParent is not null)
                                                {
                                                    await localParent.LoadChildListAsync().ConfigureAwait(false);
                                                    TreeViewService.ReRenderNode(mostRecentContainer.Key, localParent);
                                                }
                                            }
                                        }
                                    })
                                .ConfigureAwait(false);
                        }
                    },
                    { nameof(IBooleanPromptOrCancelRendererType.OnAfterDeclineFunc), commandArgs.RestoreFocusToTreeView },
                    { nameof(IBooleanPromptOrCancelRendererType.OnAfterCancelFunc), commandArgs.RestoreFocusToTreeView },
                }));
        }

        if (!menuOptionList.Any())
            return MenuRecord.Empty;

        return new MenuRecord(menuOptionList.ToImmutableArray());
    }

    private MenuOptionRecord[] GetDotNetSolutionMenuOptions(TreeViewSolution treeViewSolution)
    {
        // TODO: Add menu options for non C# projects perhaps a more generic option is good

        var addNewCSharpProject = new MenuOptionRecord(
            "New C# Project",
            MenuOptionKind.Other,
            () => OpenNewCSharpProjectDialog(treeViewSolution.Item));

        var addExistingCSharpProject = new MenuOptionRecord(
            "Existing C# Project",
            MenuOptionKind.Other,
            () => AddExistingProjectToSolution(treeViewSolution.Item));

        var createOptions = new MenuOptionRecord("Add", MenuOptionKind.Create,
            SubMenu: new MenuRecord(new[]
            {
                addNewCSharpProject,
                addExistingCSharpProject,
            }.ToImmutableArray()));

        var openInTextEditor = new MenuOptionRecord(
            "Open in text editor",
            MenuOptionKind.Update,
            () => OpenSolutionInTextEditor(treeViewSolution.Item));

        return new[]
        {
            createOptions,
            openInTextEditor,
        };
    }

    private MenuOptionRecord[] GetCSharpProjectMenuOptions(TreeViewNamespacePath treeViewModel)
    {
        var parentDirectory = treeViewModel.Item.AbsolutePath.AncestorDirectoryList.Last();
        var treeViewSolution = treeViewModel.Parent as TreeViewSolution;

        if (treeViewSolution is null)
        {
            var ancestorTreeView = treeViewModel.Parent;

            if (ancestorTreeView?.Parent is null)
                return Array.Empty<MenuOptionRecord>();

            // Parent could be a could be one or many levels of solution folders
            while (ancestorTreeView.Parent is not null)
            {
                ancestorTreeView = ancestorTreeView.Parent;
            }

            treeViewSolution = ancestorTreeView as TreeViewSolution;

            if (treeViewSolution is null)
                return Array.Empty<MenuOptionRecord>();
        }

        var parentDirectoryAbsolutePath = EnvironmentProvider.AbsolutePathFactory(parentDirectory.Value, true);

        return new[]
        {
            MenuOptionsFactory.NewEmptyFile(
                parentDirectoryAbsolutePath,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.NewTemplatedFile(
                new NamespacePath(treeViewModel.Item.Namespace, parentDirectoryAbsolutePath),
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.NewDirectory(
                parentDirectoryAbsolutePath,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.PasteClipboard(
                parentDirectoryAbsolutePath,
                async () =>
                {
                    var localParentOfCutFile = ParentOfCutFile;
                    ParentOfCutFile = null;

                    if (localParentOfCutFile is not null)
                        await ReloadTreeViewModel(localParentOfCutFile).ConfigureAwait(false);

                    await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false);
                }),
            MenuOptionsFactory.AddProjectToProjectReference(
                treeViewModel,
                TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY],
                Dispatcher,
                IdeBackgroundTaskApi,
                () => Task.CompletedTask),
            MenuOptionsFactory.MoveProjectToSolutionFolder(
                treeViewSolution,
                treeViewModel,
                TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY],
                Dispatcher,
                async () =>
                {
                    await IdeBackgroundTaskApi.DotNetSolution
                        .SetDotNetSolution(treeViewSolution.Item.NamespacePath.AbsolutePath)
                        .ConfigureAwait(false);
                }),
            new MenuOptionRecord(
                "Set as Startup Project",
                MenuOptionKind.Other,
                () => 
                {
                    Dispatcher.Dispatch(new ProgramExecutionState.SetStartupProjectAbsolutePathAction(treeViewModel.Item.AbsolutePath));
                    return Task.CompletedTask;
                }),
            MenuOptionsFactory.RemoveCSharpProjectReferenceFromSolution(
                treeViewSolution,
                treeViewModel,
                TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY],
                Dispatcher,
                async () =>
                {
                    await IdeBackgroundTaskApi.DotNetSolution
                        .SetDotNetSolution(treeViewSolution.Item.NamespacePath.AbsolutePath)
                        .ConfigureAwait(false);
                }),
        };
    }

    private MenuOptionRecord[] GetCSharpProjectToProjectReferenceMenuOptions(
        TreeViewCSharpProjectToProjectReference treeViewCSharpProjectToProjectReference)
    {
        return new[]
        {
            MenuOptionsFactory.RemoveProjectToProjectReference(
                treeViewCSharpProjectToProjectReference,
                TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY],
                Dispatcher, () => Task.CompletedTask),
        };
    }

    private MenuOptionRecord[] GetTreeViewLightWeightNugetPackageRecordMenuOptions(
        TreeViewCSharpProjectNugetPackageReference treeViewCSharpProjectNugetPackageReference)
    {
        if (treeViewCSharpProjectNugetPackageReference.Parent 
                is not TreeViewCSharpProjectNugetPackageReferences treeViewCSharpProjectNugetPackageReferences)
        {
            return MenuRecord.Empty.MenuOptionList.ToArray();
        }
        
        return new[]
        {
            MenuOptionsFactory.RemoveNuGetPackageReferenceFromProject(
                treeViewCSharpProjectNugetPackageReferences.Item.CSharpProjectNamespacePath,
                treeViewCSharpProjectNugetPackageReference,
                TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY],
                Dispatcher, () => Task.CompletedTask),
        };
    }

    private MenuOptionRecord[] GetDirectoryMenuOptions(TreeViewNamespacePath treeViewModel)
    {
        return new[]
        {
            MenuOptionsFactory.NewEmptyFile(
                treeViewModel.Item.AbsolutePath,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.NewTemplatedFile(
                treeViewModel.Item,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.NewDirectory(
                treeViewModel.Item.AbsolutePath,
                async () => await ReloadTreeViewModel(treeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.PasteClipboard(
                treeViewModel.Item.AbsolutePath,
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
        TreeViewNamespacePath treeViewModel,
        TreeViewNamespacePath? parentTreeViewModel)
    {
        return new[]
        {
            MenuOptionsFactory.CopyFile(
                treeViewModel.Item.AbsolutePath,
                () => {
                    NotificationHelper.DispatchInformative("Copy Action", $"Copied: {treeViewModel.Item.AbsolutePath.NameWithExtension}", CommonComponentRenderers, Dispatcher, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                }),
            MenuOptionsFactory.CutFile(
                treeViewModel.Item.AbsolutePath,
                () => {
                    ParentOfCutFile = parentTreeViewModel;
                    NotificationHelper.DispatchInformative("Cut Action", $"Cut: {treeViewModel.Item.AbsolutePath.NameWithExtension}", CommonComponentRenderers, Dispatcher, TimeSpan.FromSeconds(7));
                    return Task.CompletedTask;
                }),
            MenuOptionsFactory.DeleteFile(
                treeViewModel.Item.AbsolutePath,
                async () => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false)),
            MenuOptionsFactory.RenameFile(
                treeViewModel.Item.AbsolutePath,
                Dispatcher,
                async ()  => await ReloadTreeViewModel(parentTreeViewModel).ConfigureAwait(false)),
        };
    }

    private MenuOptionRecord[] GetDebugMenuOptions(TreeViewNamespacePath treeViewModel)
    {
        return new MenuOptionRecord[]
        {
            // new MenuOptionRecord(
            //     $"namespace: {treeViewModel.Item.Namespace}",
            //     MenuOptionKind.Read)
        };
    }

    private Task OpenNewCSharpProjectDialog(DotNetSolutionModel dotNetSolutionModel)
    {
        var dialogRecord = new DialogViewModel(
            _newCSharpProjectDialogKey,
            "New C# Project",
            typeof(CSharpProjectFormDisplay),
            new Dictionary<string, object?>
            {
                {
                    nameof(CSharpProjectFormDisplay.DotNetSolutionModelKey),
                    dotNetSolutionModel.Key
                },
            },
            null,
			true);

        Dispatcher.Dispatch(new DialogState.RegisterAction(dialogRecord));
        return Task.CompletedTask;
    }

    private Task AddExistingProjectToSolution(DotNetSolutionModel dotNetSolutionModel)
    {
        return IdeBackgroundTaskApi.InputFile.RequestInputFileStateForm(
            "Existing C# Project to add to solution",
            async absolutePath =>
            {
                if (absolutePath is null)
                    return;

                var localFormattedAddExistingProjectToSolutionCommand = DotNetCliCommandFormatter.FormatAddExistingProjectToSolution(
                        dotNetSolutionModel.NamespacePath.AbsolutePath.Value,
                        absolutePath.Value);

                var addExistingProjectToSolutionTerminalCommand = new TerminalCommand(
                    Key<TerminalCommand>.NewKey(),
                    localFormattedAddExistingProjectToSolutionCommand,
                    null,
                    CancellationToken.None,
                    async () =>
                    {
                        await IdeBackgroundTaskApi.DotNetSolution
                            .SetDotNetSolution(dotNetSolutionModel.NamespacePath.AbsolutePath)
                            .ConfigureAwait(false);
                    });

                var generalTerminal = TerminalStateWrap.Value.TerminalMap[TerminalFacts.GENERAL_TERMINAL_KEY];
                await generalTerminal
                    .EnqueueCommandAsync(addExistingProjectToSolutionTerminalCommand)
                    .ConfigureAwait(false);
            },
            absolutePath =>
            {
                if (absolutePath is null || absolutePath.IsDirectory)
                    return Task.FromResult(false);

                return Task.FromResult(absolutePath.ExtensionNoPeriod.EndsWith(ExtensionNoPeriodFacts.C_SHARP_PROJECT));
            },
            new[]
            {
                new InputFilePattern(
                    "C# Project",
                    absolutePath => absolutePath.ExtensionNoPeriod.EndsWith(ExtensionNoPeriodFacts.C_SHARP_PROJECT))
            }.ToImmutableArray());
    }

    private async Task OpenSolutionInTextEditor(DotNetSolutionModel dotNetSolutionModel)
    {
        var resourceUri = new ResourceUri(dotNetSolutionModel.AbsolutePath.Value);

        if (TextEditorConfig.RegisterModelFunc is null)
            return;

        await TextEditorConfig.RegisterModelFunc.Invoke(new RegisterModelArgs(
                resourceUri,
                ServiceProvider))
            .ConfigureAwait(false);

        if (TextEditorConfig.TryRegisterViewModelFunc is not null)
        {
            var viewModelKey = await TextEditorConfig.TryRegisterViewModelFunc.Invoke(new TryRegisterViewModelArgs(
                    Key<TextEditorViewModel>.NewKey(),
                    resourceUri,
                    new Category("main"),
                    false,
                    ServiceProvider))
                .ConfigureAwait(false);

            if (viewModelKey != Key<TextEditorViewModel>.Empty &&
                TextEditorConfig.TryShowViewModelFunc is not null)
            {
                await TextEditorConfig.TryShowViewModelFunc.Invoke(new TryShowViewModelArgs(
                        viewModelKey,
                        Key<TextEditorGroup>.Empty,
                        ServiceProvider))
                    .ConfigureAwait(false);
            }
        }
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

        TreeViewService.ReRenderNode(DotNetSolutionState.TreeViewSolutionExplorerStateKey, treeViewModel);
        
		TreeViewService.MoveUp(
			DotNetSolutionState.TreeViewSolutionExplorerStateKey,
			false,
			false);
    }

    public static string GetContextMenuCssStyleString(TreeViewCommandArgs? commandArgs)
    {
        if (commandArgs?.ContextMenuFixedPosition is null)
            return "display: none;";

        var left =
            $"left: {commandArgs.ContextMenuFixedPosition.LeftPositionInPixels.ToCssValue()}px;";

        var top =
            $"top: {commandArgs.ContextMenuFixedPosition.TopPositionInPixels.ToCssValue()}px;";

        return $"{left} {top}";
    }
}