namespace BarPlay.Models;

public sealed class TaskbarWidthOption
{
    public TaskbarWidth Width { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool IsChecked { get; init; }
}
