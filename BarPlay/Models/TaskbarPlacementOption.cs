using Deskband11Lib.Core;

namespace BarPlay.Models;

public sealed class TaskbarPlacementOption
{
    public TaskbarContentPlacement Placement { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool IsChecked { get; init; }
}
