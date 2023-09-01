using System.Diagnostics;

namespace Scaffold.Tests.Utils;

public static class TimingHelpers
{
    public static double WaitMultiplier = Debugger.IsAttached ? 10000 : 1;

    public enum WaitIterationResult
    {
        Stop,
        Continue
    }

    public static async Task AsyncSpinWait(Func<Task<WaitIterationResult>> callback, string? because = null)
    {
        var waitingTime = Stopwatch.StartNew();
        var found = false;
        while (waitingTime.Elapsed.TotalSeconds < 5 * WaitMultiplier)
        {
            var r = await callback();
            if (r == WaitIterationResult.Stop)
            {
                found = true;
                break;
            }

            await Task.Delay(10);
        }

        if (!found)
        {
            throw new TimeoutException(because);
        }
    }

    public static Task AsyncSpinWaitUntil(Func<Task<bool>> callback, string? because = null)
    {
        return AsyncSpinWait(async () =>
        {
            var r = await callback();
            return r ? WaitIterationResult.Stop : WaitIterationResult.Continue;
        }, because);
    }
}