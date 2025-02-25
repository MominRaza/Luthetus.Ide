using Fluxor;
using Fluxor.Blazor.Web.Components;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Ide.RazorLib.CommandLines.Models;
using Luthetus.Ide.RazorLib.ProgramExecutions.States;
using Luthetus.Ide.RazorLib.Terminals.States;
using Luthetus.Ide.RazorLib.Terminals.Models;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Ide.RazorLib.StartupControls.Displays;

public partial class StartupControlsDisplay : FluxorComponent
{
    [Inject]
    private IState<ProgramExecutionState> ProgramExecutionStateWrap { get; set; } = null!;
    [Inject]
    private IState<TerminalState> TerminalStateWrap { get; set; } = null!;
    [Inject]
    private DotNetCliOutputParser DotNetCliOutputParser { get; set; } = null!;

    private readonly Key<TerminalCommand> _newDotNetSolutionTerminalCommandKey = Key<TerminalCommand>.NewKey();
    private readonly CancellationTokenSource _newDotNetSolutionCancellationTokenSource = new();

    private TerminalCommand? GetStartProgramTerminalCommand()
    {
        var programExecutionState = ProgramExecutionStateWrap.Value;

        if (programExecutionState.StartupProjectAbsolutePath is null)
            return null;

        var ancestorDirectory = programExecutionState.StartupProjectAbsolutePath.ParentDirectory;

        if (ancestorDirectory is null)
            return null;

        var formattedCommand = DotNetCliCommandFormatter.FormatStartProjectWithoutDebugging(
            programExecutionState.StartupProjectAbsolutePath);

        return new TerminalCommand(
            _newDotNetSolutionTerminalCommandKey,
            formattedCommand,
            ancestorDirectory.Value,
            _newDotNetSolutionCancellationTokenSource.Token,
            OutputParser: DotNetCliOutputParser);
    }

    private async Task StartProgramWithoutDebuggingOnClick()
    {
        var startProgramTerminalCommand = GetStartProgramTerminalCommand();
        if (startProgramTerminalCommand is null)
            return;

        var executionTerminal = TerminalStateWrap.Value.TerminalMap[TerminalFacts.EXECUTION_TERMINAL_KEY];
        await executionTerminal
            .EnqueueCommandAsync(startProgramTerminalCommand)
            .ConfigureAwait(false);
    }
}