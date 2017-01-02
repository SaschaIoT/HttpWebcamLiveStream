using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpWebcamLiveStream.Helper
{
    public static class TaskHelper
    {
        public static async Task CancelTaskAfterTimeout(Func<CancellationToken, Task> operation, TimeSpan timeout)
        {
            var source = new CancellationTokenSource();
            var task = operation(source.Token);
            //After task starts timeout begin to tick
            source.CancelAfter(timeout);
            await task;
        }
    }
}
