﻿using Luthetus.Common.RazorLib.JavaScriptObjects.Models;
using Microsoft.JSInterop;

namespace Luthetus.Common.RazorLib.JsRuntimes.Models;

public class LuthetusCommonJavaScriptInteropApi
{
    private readonly IJSRuntime _jsRuntime;

    public LuthetusCommonJavaScriptInteropApi(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask FocusHtmlElementById(string elementId)
    {
        return _jsRuntime.InvokeVoidAsync(
            "luthetusCommon.focusHtmlElementById",
            elementId);
    }

    public ValueTask<bool> TryFocusHtmlElementById(string elementId)
    {
        return _jsRuntime.InvokeAsync<bool>(
            "luthetusCommon.tryFocusHtmlElementById",
            elementId);
    }
    
    public ValueTask<MeasuredHtmlElementDimensions> MeasureElementById(string elementId)
    {
        return _jsRuntime.InvokeAsync<MeasuredHtmlElementDimensions>(
            "luthetusCommon.measureElementById",
            elementId);
    }

    public ValueTask LocalStorageSetItem(string key, object? value)
    {
        return _jsRuntime.InvokeVoidAsync(
            "luthetusCommon.localStorageSetItem",
            key,
            value);
    }

    public ValueTask<string?> LocalStorageGetItem(string key)
    {
        return _jsRuntime.InvokeAsync<string?>(
            "luthetusCommon.localStorageGetItem",
            key);
    }

    public ValueTask<string> ReadClipboard()
    {
        return _jsRuntime.InvokeAsync<string>(
            "luthetusCommon.readClipboard");
    }

    public ValueTask SetClipboard(string value)
    {
        return _jsRuntime.InvokeVoidAsync(
            "luthetusCommon.setClipboard",
            value);
    }

    public ValueTask<ContextMenuFixedPosition> GetTreeViewContextMenuFixedPosition(
        string nodeElementId)
    {
        return _jsRuntime.InvokeAsync<ContextMenuFixedPosition>(
            "luthetusCommon.getTreeViewContextMenuFixedPosition",
            nodeElementId);
    }
}
