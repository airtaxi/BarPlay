namespace BarPlay.Models;

public enum TaskbarWidth
{
    Narrow,
    Normal,
    Wide,
    FillRemainingSpace
}

public static class TaskbarWidthExtensions
{
    public static double GetPreferredWidth(this TaskbarWidth width) => width switch
    {
        TaskbarWidth.Narrow => 138,
        TaskbarWidth.Normal => 280,
        TaskbarWidth.Wide => 380,
        TaskbarWidth.FillRemainingSpace => double.MaxValue,
        _ => 380
    };
}
