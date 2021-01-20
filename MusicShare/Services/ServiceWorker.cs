using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MusicShare.Interaction.Standard.Common;

namespace MusicShare.Services
{
    public abstract class ServiceCommand<TCmd, TSvc>
        where TCmd : ServiceCommand<TCmd, TSvc>
    {
        public void HandleWith(TSvc handler)
        {
            this.HandleWithImpl(handler);
        }

        protected abstract void HandleWithImpl(TSvc svc);
    }

    public abstract class ServiceWorker<TCmd, TSvc> : DisposableObject
        where TCmd : ServiceCommand<TCmd, TSvc>
    {
        private readonly Thread _thread;
        private bool _working = true;
        private readonly object _commandsLock = new object();
        private readonly Queue<TCmd> _commands = new Queue<TCmd>();

        protected abstract TSvc Interpreter { get; }

        public ServiceWorker()
        {
            _thread = new Thread(this.WorkerThreadProc);
        }

        public void Start()
        {
            _thread.Start();
        }

        private void WorkerThreadProc()
        {
            while (this.TryWaitForNextCommand(out var cmd))
            {
                cmd.HandleWith(this.Interpreter);
            }
        }

        private bool TryWaitForNextCommand(out TCmd cmd)
        {
            lock (_commandsLock)
            {
                while (_working)
                {
                    if (_commands.Count > 0)
                    {
                        cmd = _commands.Dequeue();
                        return true;
                    }

                    Monitor.Wait(_commandsLock);
                }

                cmd = default(TCmd);
                return false;
            }
        }

        public void Post(TCmd cmd)
        {
            lock (_commandsLock)
            {
                _commands.Enqueue(cmd);
                Monitor.Pulse(_commandsLock);
            }
        }

        protected sealed override void DisposeImpl()
        {
            _working = false;

            this.Cleanup();
        }

        protected abstract void Cleanup();
    }
}
