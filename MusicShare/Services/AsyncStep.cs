//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Threading;

//namespace MusicShare.Services
//{
//    public interface IFlattenable<T> : IEnumerable<T>
//        where T : IFlattenable<T>
//    {
//    }

//    public class AsyncContextsDispatcher
//    {
//        private class Context
//        {
//            private readonly Stack<IEnumerator<AsyncStep>> _stack = new Stack<IEnumerator<AsyncStep>>();

//            public WaitHandle Waiter { get; private set; }

//            public Context(AsyncStep step)
//            {
//                IEnumerable<AsyncStep> Ctx() { yield return step; }

//                var stack = new Stack<IEnumerator<AsyncStep>>();
//                stack.Push(Ctx().GetEnumerator());

//            }

//            private bool UnwindExeption(out Action<Exception> handler)
//            {
//                handler = _stack.Peek().Current.OnExceptionCallback;
//                while (handler == null && _stack.Count > 1)
//                {
//                    _stack.Pop();
//                    handler = _stack.Peek().Current.OnExceptionCallback;
//                }
//                if (handler == null)
//                {
//                    handler = ex => throw new Exception("Unhandled exception", ex);
//                    return false;
//                }
//                else
//                {
//                    return true;
//                }
//            }

//            public (bool, WaitHandle) DoStep()
//            {
//                var ops = _stack.Peek();
//                WaitHandle waiter;
//                bool ok;

//                try { ok = ops.MoveNext(); }
//                catch (Exception ex1) { ok = this.UnwindExeption(out var handler); handler(ex1); }

//                if (ok)
//                {
//                    var next = ops.Current.GetEnumerator();
//                    try { ok = next.MoveNext(); }
//                    catch (Exception ex2) { ok = this.UnwindExeption(out var handler); handler(ex2); }

//                    if (ok)
//                    {
//                        _stack.Push(next);
//                        waiter = next.Current.WaitHandle;
//                    }
//                    else
//                    {
//                        waiter = ops.Current.WaitHandle;
//                    }
//                }
//                else
//                {
//                    if (_stack.Count > 0)
//                    {
//                        _stack.Pop();
//                        waiter = ops.Current.WaitHandle;
//                    }
//                    else
//                    {
//                        waiter = null;
//                    }
//                }

//                return (waiter != null, waiter);
//            }
//        }

//        private readonly object _lock = new object();
//        private readonly AutoResetEvent _contextsUpdated = new AutoResetEvent(false);

//        private Context[] _contexts;
//        private WaitHandle[] _waiters;
//        private volatile int _count = 0;

//        public AsyncContextsDispatcher()
//        {
//            _contexts = new Context[] { null };
//            _waiters = new WaitHandle[] { AsyncStep.NeverWaiter };
//        }

//        public void PostWork(AsyncStep step)
//        {
//            lock (_lock)
//            {
//                var index = _count;
//                _count++;

//                if (_count > _contexts.Length)
//                {
//                    Array.Resize(ref _contexts, _count);
//                    Array.Resize(ref _waiters, _count);
//                }

//                _waiters[index] = (_contexts[index] = new Context(step)).Waiter;
//            }
//        }

//        private void ReleaseContext(int index)
//        {
//            if (_contexts.Length > 1)
//            {
//                _contexts[index] = _contexts[_contexts.Length - 1];
//                _waiters[index] = _waiters[_waiters.Length - 1];
//            }

//            _contexts[_contexts.Length - 1] = null;
//            _waiters[_waiters.Length - 1] = AsyncStep.NeverWaiter;
//            _count--;
//        }

//        public void DoWork()
//        {
//            for (; ; )
//            {
//                int index;
//                Context context;

//                Context[] contexts;
//                WaitHandle[] waiters;

//                do
//                {
//                    lock (_lock)
//                    {
//                        contexts = _contexts;
//                        waiters = _waiters;
//                    }

//                    index = WaitHandle.WaitAny(waiters);
//                    context = contexts[index];
//                } while (index == 0);

//                var (ok, waiter) = context.DoStep();
//                lock (_lock)
//                {
//                    if (ok)
//                        _waiters[index] = waiter;
//                    else
//                        this.ReleaseContext(index);
//                }
//            }
//        }
//    }

//    public class AsyncStep : IFlattenable<AsyncStep>
//    {
//        private static readonly AsyncStep[] _noSubsteps = new AsyncStep[0];

//        public static WaitHandle AccomplishedWaiter { get; } = new ManualResetEvent(true);
//        public static WaitHandle NeverWaiter { get; } = new ManualResetEvent(false);

//        public WaitHandle WaitHandle { get; }
//        public IEnumerable<AsyncStep> Substeps { get; }

//        public Action<Exception> OnExceptionCallback { get; }

//        public AsyncStep(WaitHandle waitHandle, Action<Exception> onEx = null)
//        {
//            this.WaitHandle = waitHandle;
//            this.Substeps = _noSubsteps;
//            this.OnExceptionCallback = onEx;
//        }

//        public AsyncStep(IEnumerable<AsyncStep> substeps, Action<Exception> onEx = null)
//        {
//            this.WaitHandle = AsyncStep.AccomplishedWaiter;
//            this.Substeps = substeps;
//            this.OnExceptionCallback = onEx;
//        }

//        public IEnumerator<AsyncStep> GetEnumerator() { return this.Substeps.GetEnumerator(); }

//        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
//    }

//    internal class AsyncStepResult
//    {
//        public bool Successed { get; private set; }
//        public Exception Exception { get; private set; }

//        public AsyncStepResult()
//        {
//        }

//        internal void SetFailed(Exception ex)
//        {
//            this.Successed = false;
//            this.Exception = ex;
//        }

//        internal void SetSuccess()
//        {
//            this.Successed = true;
//            this.Exception = null;
//        }

//        //public void RethrowIfError()
//        //{
//        //    if (this.Exception != null)
//        //        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(this.Exception).Throw();
//        //}
//    }

//    internal static class AsyncStepExtensions
//    {
//        public static AsyncStep BeginWriteAsyncStep(this Stream stream, byte[] data, out IAsyncResult ar)
//        {
//            ar = stream.BeginWrite(data, 0, data.Length, null, null);
//            return new AsyncStep(ar.AsyncWaitHandle);
//        }

//        public static AsyncStep WriteAsyncStep(this Stream stream, byte[] data, AsyncStepResult result)
//        {
//            IEnumerable<AsyncStep> Impl()
//            {
//                IAsyncResult ar;

//                try { ar = stream.BeginWrite(data, 0, data.Length, null, null); }
//                catch (Exception e1) { result.SetFailed(e1); yield break; }
//                yield return new AsyncStep(ar.AsyncWaitHandle);

//                try { stream.EndWrite(ar); }
//                catch (Exception e2) { result.SetFailed(e2); yield break; }

//                result.SetSuccess();
//            }

//            return new AsyncStep(Impl());
//        }


//        public static AsyncStep WriteAsyncStep(this Stream stream, byte[] data)
//        {
//            IEnumerable<AsyncStep> Impl()
//            {
//                yield return stream.BeginWriteAsyncStep(data, out var ar);
//                stream.EndWrite(ar);
//            }

//            return new AsyncStep(Impl());
//        }

//        public static AsyncStep BeginReadAsyncStep(this Stream stream, byte[] data, int offset, int count, out IAsyncResult ar)
//        {
//            ar = stream.BeginRead(data, offset, count, null, null);
//            return new AsyncStep(ar.AsyncWaitHandle);
//        }

//        public static AsyncStep ReadAsyncStep(this Stream stream, byte[] data, AsyncStepResult result)
//        {
//            IEnumerable<AsyncStep> Impl()
//            {
//                int rcvd = 0, step;
//                while (rcvd < data.Length)
//                {
//                    IAsyncResult ar;
//                    try { ar = stream.BeginRead(data, rcvd, data.Length - rcvd, null, null); }
//                    catch (Exception e1) { result.SetFailed(e1); yield break; }
//                    yield return new AsyncStep(ar.AsyncWaitHandle);

//                    try { step = stream.EndRead(ar); }
//                    catch (Exception e2) { result.SetFailed(e2); yield break; }

//                    try { if (step == 0) throw new EndOfStreamException($"Failed to read {data.Length} bytes from stream due to EOF being reached"); }
//                    catch (Exception e3) { result.SetFailed(e3); yield break; }

//                    rcvd += step;
//                }

//                result.SetSuccess();
//            }

//            return new AsyncStep(Impl());
//        }

//        public static AsyncStep ReadAsyncStep(this Stream stream, byte[] data)
//        {
//            IEnumerable<AsyncStep> Impl()
//            {
//                var rcvd = 0;
//                while (rcvd < data.Length)
//                {
//                    yield return stream.BeginReadAsyncStep(data, rcvd, data.Length - rcvd, out var ar);
//                    var has = stream.EndRead(ar);
//                    if (has == 0)
//                        throw new EndOfStreamException($"Failed to read {data.Length} bytes from stream");

//                    rcvd += has;
//                }
//            }

//            return new AsyncStep(Impl());
//        }
//    }
//}
