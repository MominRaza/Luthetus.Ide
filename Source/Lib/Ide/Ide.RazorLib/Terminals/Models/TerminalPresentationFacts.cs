﻿using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Ide.RazorLib.Terminals.Models;
using Luthetus.TextEditor.RazorLib.Decorations.Models;

namespace Luthetus.TextEditor.RazorLib.TextEditors.Models.Internals;

public static class TerminalPresentationFacts
{
    public const string CssClassString = "luth_te_terminal-presentation";

    public static readonly Key<TextEditorPresentationModel> PresentationKey = Key<TextEditorPresentationModel>.NewKey();

    public static readonly TextEditorPresentationModel EmptyPresentationModel = new(
        PresentationKey,
        0,
        CssClassString,
        new TerminalDecorationMapper());
}
