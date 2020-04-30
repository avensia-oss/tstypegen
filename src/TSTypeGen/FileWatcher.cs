using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TSTypeGen
{
    public static class FileWatcher
    {
        private static FileSystemEventArgs MergeChanges(FileSystemEventArgs a, FileSystemEventArgs b)
        {
            if (a.ChangeType == WatcherChangeTypes.Deleted && b.ChangeType == WatcherChangeTypes.Created)
                return new FileSystemEventArgs(WatcherChangeTypes.Changed, a.FullPath.Substring(0, a.FullPath.Length - a.Name.Length), a.Name);
            else
                return b;
        }

        private static void OnChangeInvokerThreadProc(WaitHandle workWaitHandle, WaitHandle exitWaitHandle, TimeSpan toWait, Dictionary<string, Tuple<DateTime, FileSystemEventArgs>> workItems, Func<FileSystemEventArgs, Task> onChange, Action<Exception> onError)
        {
            var waitHandles = new[] { exitWaitHandle, workWaitHandle };
            for (;;)
            {
                var waitResult = WaitHandle.WaitAny(waitHandles);

                for (;;)
                {
                    Monitor.Enter(workItems);

                    if (workItems.Count == 0)
                    {
                        Monitor.Exit(workItems);
                        break;
                    }

                    var nextChange = workItems.Values.OrderBy(x => x.Item1).First();
                    TimeSpan toSleep = nextChange.Item1 + toWait - DateTime.Now;
                    if (toSleep > TimeSpan.Zero)
                    {
                        Monitor.Exit(workItems);
                        Thread.Sleep(toSleep);
                    }
                    else
                    {
                        workItems.Remove(nextChange.Item2.FullPath);
                        Monitor.Exit(workItems);
                        try
                        {
                            onChange(nextChange.Item2).Wait();
                        }
                        catch (AggregateException ex)
                        {
                            onError(ex.InnerException);
                        }
                        catch (Exception ex)
                        {
                            onError(ex);
                        }
                    }
                }

                if (waitResult == 0)
                {
                    return;
                }
            }
        }

        private static bool IsDirectory(string fullPath)
        {
            try
            {
                return File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static Task WatchPath(string path, TimeSpan toWait, CancellationToken cancellationToken, Func<FileSystemEventArgs, Task> onChange, Action<Exception> onError, Action watchStarted)
        {
            var workItems = new Dictionary<string, Tuple<DateTime, FileSystemEventArgs>>(StringComparer.OrdinalIgnoreCase);

            var workEvent = new AutoResetEvent(false);
            var exitEvent = new ManualResetEvent(false);
            var workerThread = new Thread(() => OnChangeInvokerThreadProc(workEvent, exitEvent, toWait, workItems, onChange, onError));

            workerThread.Start();

            var cts = new TaskCompletionSource<bool>();

            void ChangeHandler(FileSystemEventArgs change)
            {
                if (IsDirectory(change.FullPath)) return;

                lock (workItems)
                {
                    workItems.TryGetValue(change.FullPath, out var old);
                    workItems[change.FullPath] = Tuple.Create(DateTime.Now, old != null ? MergeChanges(old.Item2, change) : change);
                }

                workEvent.Set();
            }

            var watchers = new List<FileSystemWatcher>();
            foreach (var pattern in new List<string> {"*.cs", "*.csproj", "*.sln"})
            {
                var watcher = new FileSystemWatcher(path)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    Filter = pattern,
                };

                watcher.Created += (_, e) => ChangeHandler(e);
                watcher.Renamed += (_, e) =>
                {
                    ChangeHandler(new FileSystemEventArgs(WatcherChangeTypes.Deleted,
                        e.OldFullPath.Substring(0, e.OldFullPath.Length - e.OldName.Length), e.OldName));
                    ChangeHandler(new FileSystemEventArgs(WatcherChangeTypes.Created,
                        e.FullPath.Substring(0, e.FullPath.Length - e.Name.Length), e.Name));
                };
                watcher.Changed += (_, e) => ChangeHandler(e);
                watcher.Deleted += (_, e) => ChangeHandler(e);
                watcher.Error += (_, e) => onError(e.GetException());

                watchers.Add(watcher);
            }

            watchStarted();

            cancellationToken.Register(() =>
            {
                foreach (var watcher in watchers)
                {
                    watcher.Dispose();
                }
                exitEvent.Set();
                workerThread.Join();
                workEvent.Dispose();
                exitEvent.Dispose();

                cts.SetResult(true);
            });

            return cts.Task;
        }
    }
}
