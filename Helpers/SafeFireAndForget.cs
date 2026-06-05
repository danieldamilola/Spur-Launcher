using Spur.Services;
using System;
using System.Threading.Tasks;

namespace Spur.Helpers;

/// <summary>
/// Safe wrapper for fire-and-forget Task calls.
/// Ensures exceptions in the continuation (including inside catch blocks)
/// don't crash the process via UnobservedTaskException.
/// </summary>
public static class SafeFireAndForget
{
    /// <summary>
    /// Runs the task and logs any unhandled exception to the optional logger.
    /// Never throws — even if the logger itself throws.
    /// </summary>
    public static async void Run(Func<Task> taskFactory, ILogger? logger = null, string? context = null)
    {
        try
        {
            await taskFactory();
        }
        catch (Exception ex)
        {
            try
            {
                logger?.Error($"Fire-and-forget task failed{(context is not null ? $" [{context}]" : "")}: {ex.Message}", ex);
            }
            catch
            {
                // Must never throw — this is fire-and-forget
            }
        }
    }
}
