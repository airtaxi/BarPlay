using Windows.ApplicationModel;

namespace BarPlay.Services;

public sealed class StartupTaskService : IStartupTaskService
{
    private const string TaskId = "BarPlayStartupTask";

    public async Task<bool> IsEnabledAsync()
    {
        var tasks = await StartupTask.GetForCurrentPackageAsync();
        var task = tasks.FirstOrDefault(candidate => candidate.TaskId == TaskId);
        if (task is null) return false;
        return task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
    }

    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        var tasks = await StartupTask.GetForCurrentPackageAsync();
        var task = tasks.FirstOrDefault(candidate => candidate.TaskId == TaskId);
        if (task is null) return false;

        if (enabled)
        {
            var result = await task.RequestEnableAsync();
            return result is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }

        task.Disable();
        return false;
    }
}
