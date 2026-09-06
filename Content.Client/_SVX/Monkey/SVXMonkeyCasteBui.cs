using System.Linq;
using Content.Shared._SVX.Monkey;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._SVX.Monkey;

[UsedImplicitly]
public sealed class SVXMonkeyCasteBui : BoundUserInterface
{
    [ViewVariables]
    private SVXMonkeyCasteWindow? _window;

    public SVXMonkeyCasteBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SVXMonkeyCasteWindow>();
        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        Refresh();
    }

    private void Refresh()
    {
        if (_window == null)
            return;

        _window.CasteList.RemoveAllChildren();

        if (State is not SVXMonkeyCasteBuiState { } st)
            return;

        var sorted = st.Options;
        foreach (var option in sorted)
        {
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };

            var label = new Label
            {
                Text = option.DisplayName,
                HorizontalExpand = true,
                Modulate = option.Unlocked
                    ? Color.White
                    : Color.Gray,
            };

            if (option.Current)
                label.Text += "  (selected)";
            else if (!option.Unlocked && !string.IsNullOrEmpty(option.UnlockHint))
                label.Text += $"  ({option.UnlockHint})";

            var pick = new Button
            {
                Text = option.Unlocked ? "Select" : "Locked",
                Disabled = !option.Unlocked || option.Current,
            };

            var castId = option.CastId;
            pick.OnPressed += _ =>
            {
                SendMessage(new SVXMonkeyCastePickBuiMsg(castId));
            };

            row.AddChild(label);
            row.AddChild(pick);
            _window.CasteList.AddChild(row);
        }

        var info = "Select a caste to appear as on your next (re)spawn.";
        if (st.CooldownRemainingSeconds > 0)
            info += $"  (cooldown {st.CooldownRemainingSeconds:0}s)";
        _window.InfoLabel.Text = info;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Dispose();
    }
}
