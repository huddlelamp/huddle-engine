using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Huddle.Engine.Extensions
{
    public static class DispatcherExtensions
    {
        public static Task<TaskScheduler> ToTaskSchedulerAsync(this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskScheduler>();
            var invocation = dispatcher.BeginInvoke(new Action(() =>
                taskCompletionSource.SetResult(TaskScheduler.FromCurrentSynchronizationContext())), priority);

            invocation.Aborted += (s, e) =>
                taskCompletionSource.SetCanceled();

            return taskCompletionSource.Task;
        }
    }
}
