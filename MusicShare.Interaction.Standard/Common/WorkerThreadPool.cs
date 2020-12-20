using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicShare.Interaction.Standard.Common
{
    public class WorkerThreadPool : DisposableObject
    {
        class WorkItem
        {
            public readonly Action action;

            public WorkItem(Action action)
            {
                this.action = action;
            }
        }

        public int ThreadsCount { get; private set; }

        readonly object _lock = new object();
        readonly Queue<WorkItem> _workItems = new Queue<WorkItem>();

        readonly List<Thread> _threads = new List<Thread>();

        public WorkerThreadPool(int threadsCount)
        {
            this.ThreadsCount = threadsCount;

            for (int i = 0; i < threadsCount; i++)
            {
                _threads.Add(new Thread(this.WorkerProc) { IsBackground = true });
            }
        }

        private void WorkerProc()
        {
            WorkItem workItem;

            while (!this.IsDisposed)
            {
                lock (_lock)
                {
                    while (_workItems.Count > 0)
                    {
                        Monitor.Wait(_lock);
                        if (this.IsDisposed)
                            return;
                    }

                    workItem = _workItems.Dequeue();
                }

                workItem.action();
            }
        }

        public void Schedule(Action act)
        {
            lock (_lock)
            {
                _workItems.Enqueue(new WorkItem(act));
                Monitor.Pulse(_lock);
            }
        }

        protected override void DisposeImpl()
        {
            lock (_lock)
            {
                Monitor.PulseAll(_lock);
            }
        }
    }
}
