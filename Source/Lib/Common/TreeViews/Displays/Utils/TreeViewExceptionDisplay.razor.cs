﻿using Luthetus.Common.RazorLib.ComponentRenderers.Models;
using Luthetus.Common.RazorLib.WatchWindows.Models;
using Microsoft.AspNetCore.Components;

namespace Luthetus.Common.RazorLib.TreeViews.Displays.Utils;

public partial class TreeViewExceptionDisplay : ComponentBase, ITreeViewExceptionRendererType
{
    [Parameter, EditorRequired]
    public TreeViewException TreeViewException { get; set; } = null!;
}