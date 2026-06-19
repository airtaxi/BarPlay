namespace BarPlay.Services;

public interface IStartupTaskService
{
    Task<bool> IsEnabledAsync();

    Task<bool> SetEnabledAsync(bool enabled);
}
