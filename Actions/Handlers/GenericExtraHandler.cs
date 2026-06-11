using Spur.Extensions;
using Spur.Models;
using Spur.Services;

namespace Spur.Actions.Handlers;

public sealed class GenericExtraHandler : IActionHandler
{
    private readonly ExtrasRegistry _extras;
    private readonly IClipboardService _clipboard;

    public GenericExtraHandler(ExtrasRegistry extras, IClipboardService clipboard)
    {
        _extras = extras ?? throw new ArgumentNullException(nameof(extras));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    }

    public bool CanHandle(SearchResult result) =>
        !string.IsNullOrEmpty(result.ActionId) && _extras.FindById(result.ActionId) is not null;

    public async Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        if (result.ActionId is null) return ActionPanelState.Empty();

        var extra = _extras.FindById(result.ActionId);
        if (extra is null) return ActionPanelState.Empty();

        var extraResult = await extra.ExecuteAsync(actionInput);

        if (extraResult.Success)
        {
            if (!string.IsNullOrEmpty(extraResult.CopyText))
                _clipboard.CopyTextToSystem(extraResult.CopyText);

            return new ActionPanelState
            {
                PanelId = extraResult.PanelId,
                Title = extraResult.Title,
                Subtitle = actionInput,
                State = !string.IsNullOrEmpty(extraResult.CopyText) ? "Copied" : "Completed",
                ResultText = extraResult.Detail,
                ResultSubText = extraResult.SubText
            };
        }

        return new ActionPanelState
        {
            PanelId = extraResult.PanelId,
            Title = extraResult.Title,
            Subtitle = actionInput,
            State = "Error",
            ResultText = string.IsNullOrEmpty(extraResult.Detail) ? "Failed" : extraResult.Detail,
            ResultSubText = extraResult.SubText
        };
    }
}
