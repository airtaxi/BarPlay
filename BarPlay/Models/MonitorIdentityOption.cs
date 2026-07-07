namespace BarPlay.Models;

public sealed class MonitorIdentityOption
{
    public int Identity { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool IsChecked { get; init; }
}