using Luthetus.TextEditor.RazorLib.JsRuntimes.Models;
using Luthetus.TextEditor.RazorLib.Virtualizations.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Luthetus.TextEditor.RazorLib.Virtualizations.Displays;

public partial class VirtualizationDisplay : ComponentBase, IDisposable
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public IVirtualizationResultWithoutTypeMask VirtualizationResultWithoutTypeMask { get; set; } = null!;
    [Parameter, EditorRequired]
    public Func<VirtualizationRequest, Task>? ItemsProviderFunc { get; set; }

    [Parameter]
    public bool UseHorizontalVirtualization { get; set; } = true;
    [Parameter]
    public bool UseVerticalVirtualization { get; set; } = true;
    /// <summary>
    /// The Gutter for the TextEditor uses the VirtualizationDisplay. I am going to see if adding this property allows me to have the gutter be given 'vertical padding' by means of the virtualization boundaries. Yet, not any intersection observer logic.
    /// </summary>
    [Parameter]
    public bool UseIntersectionObserver { get; set; } = true;

    private readonly Guid _virtualizationDisplayGuid = Guid.NewGuid();
    private VirtualizationRequest _request = null!;
    private ElementReference _scrollableParentFinder;
    private CancellationTokenSource _scrollEventCancellationTokenSource = new();

    private string LeftBoundaryElementId => $"luth_te_left-virtualization-boundary-display-{_virtualizationDisplayGuid}";
    private string RightBoundaryElementId => $"luth_te_right-virtualization-boundary-display-{_virtualizationDisplayGuid}";
    private string TopBoundaryElementId => $"luth_te_top-virtualization-boundary-display-{_virtualizationDisplayGuid}";
    private string BottomBoundaryElementId => $"luth_te_bottom-virtualization-boundary-display-{_virtualizationDisplayGuid}";

    protected override void OnInitialized()
    {
        _scrollEventCancellationTokenSource.Cancel();
        _scrollEventCancellationTokenSource = new CancellationTokenSource();

        _request = new(new VirtualizationScrollPosition(0, 0, 0, 0), _scrollEventCancellationTokenSource.Token);

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (UseIntersectionObserver)
            {
                var boundaryIdList = new List<object>();

                if (UseHorizontalVirtualization)
                {
                    boundaryIdList.AddRange(new[]
                    {
                        LeftBoundaryElementId,
                        RightBoundaryElementId,
                    });
                }

                if (UseVerticalVirtualization)
                {
                    boundaryIdList.AddRange(new[]
                    {
                        TopBoundaryElementId,
                        BottomBoundaryElementId,
                    });
                }

                await JsRuntime.GetLuthetusTextEditorApi()
                    .InitializeVirtualizationIntersectionObserver(
                        _virtualizationDisplayGuid.ToString(),
                        DotNetObjectReference.Create(this),
                        _scrollableParentFinder,
                        boundaryIdList)
                    .ConfigureAwait(false);
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public async Task OnScrollEventAsync(VirtualizationScrollPosition scrollPosition)
    {
        _scrollEventCancellationTokenSource.Cancel();
        _scrollEventCancellationTokenSource = new CancellationTokenSource();

        _request = new VirtualizationRequest(scrollPosition, _scrollEventCancellationTokenSource.Token);

        if (ItemsProviderFunc is not null)
            await ItemsProviderFunc.Invoke(_request).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _scrollEventCancellationTokenSource.Cancel();

        if (UseIntersectionObserver)
        {
            _ = Task.Run(async () =>
            {
                await JsRuntime.GetLuthetusTextEditorApi()
                    .DisposeVirtualizationIntersectionObserver(
                        CancellationToken.None,
                        _virtualizationDisplayGuid.ToString())
                    .ConfigureAwait(false);
            }, CancellationToken.None).ConfigureAwait(false);
        }
    }
}