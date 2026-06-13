using Spur.Extensions;
using Spur.Models;
using Spur.Services;

namespace Spur.Actions.Handlers;

public sealed class GenericAddOnHandler : IActionHandler
{
    private readonly AddOnRegistry _addOns;
    private readonly IClipboardService _clipboard;

    public GenericAddOnHandler(AddOnRegistry addOns, IClipboardService clipboard)
    {
        _addOns = addOns ?? throw new ArgumentNullException(nameof(addOns));
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    }

    public bool CanHandle(SearchResult result) =>
        !string.IsNullOrEmpty(result.ActionId) && _addOns.FindById(result.ActionId) is not null;

    public async Task<ActionPanelState> ExecuteAsync(
        SearchResult result,
        string actionInput,
        CancellationToken ct = default)
    {
        if (result.ActionId is null) return ActionPanelState.Empty();

        var addOn = _addOns.FindById(result.ActionId);
        if (addOn is null) return ActionPanelState.Empty();

        var addOnResult = await addOn.ExecuteAsync(actionInput);

        if (addOnResult.Success)
        {
            if (!string.IsNullOrEmpty(addOnResult.CopyText))
                _clipboard.CopyTextToSystem(addOnResult.CopyText);

            return new ActionPanelState
            {
                PanelId = addOnResult.PanelId,
                Title = addOnResult.Title,
                Subtitle = actionInput,
                State = !string.IsNullOrEmpty(addOnResult.CopyText) ? "Copied" : "Completed",
                ResultText = addOnResult.Detail,
                ResultSubText = addOnResult.SubText
            };
        }

        return new ActionPanelState
        {
            PanelId = addOnResult.PanelId,
            Title = addOnResult.Title,
            Subtitle = actionInput,
            State = "Error",
            ResultText = string.IsNullOrEmpty(addOnResult.Detail) ? "Failed" : addOnResult.Detail,
            ResultSubText = addOnResult.SubText
        };
    }
}
