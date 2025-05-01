using System;
using System.Threading;

namespace GoogleDrivePushCli.Utilities;

public static class RetryHelper
{
    public static T Retry<T>(Func<T> action, int maxAttempts = 3, int delayMilliseconds = 1000)
    {
        Exception lastException = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxAttempts)
                {
                    ConsoleHelpers.Info($"Attempt {attempt} failed: {ex.Message}. Retrying in {delayMilliseconds} ms...");
                    Thread.Sleep(delayMilliseconds);
                }
            }
        }
        throw new Exception($"Operation failed after {maxAttempts} attempts.", lastException);
    }
}
