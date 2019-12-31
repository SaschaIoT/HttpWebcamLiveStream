using System;
using System.Threading;
using System.Threading.Tasks;

namespace HttpWebcamLiveStream.Helper
{
    public static class TaskHelper
    {
        public static async Task WithTimeoutAfterStart(Func<CancellationToken, Task> operation, TimeSpan timeout)
        {
            var source = new CancellationTokenSource();
            var task = operation(source.Token);
            source.CancelAfter(timeout);
            await task;
        }

        public static async Task CancelTaskAfterTimeout(Func<CancellationToken, Task> operation, TimeSpan timeout)
        {
            var source = new CancellationTokenSource();
            var task = operation(source.Token);
            source.CancelAfter(timeout);
            await task;
        }
    }
}
