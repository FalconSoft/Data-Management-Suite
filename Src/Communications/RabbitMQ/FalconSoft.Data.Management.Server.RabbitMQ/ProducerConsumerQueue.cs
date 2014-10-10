using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace FalconSoft.Data.Management.Server.RabbitMQ
{
    internal class ProducerConsumerQueue<T> : IObserver<T>, IEnumerable<T>, IDisposable where T : class 
    {
        private readonly Queue<T> _queue;
        private readonly AutoResetEvent _mutex;
        private readonly object _lockThis;

        private bool _isComplete;
        private bool _isDispoced;

        public ProducerConsumerQueue()
        {
            _queue = new Queue<T>();
            _mutex = new AutoResetEvent(false);
            _lockThis = new object();

        }

        public ProducerConsumerQueue(int capisity)
        {
            _queue = new Queue<T>(capisity);
            _mutex = new AutoResetEvent(false);
            _lockThis = new object();
        }

        public void OnNext(T value)
        {
            CheckOnDispoce();

            if (value == null)
                throw new NullReferenceException("Input Value Is Null!");

            lock (_lockThis)
            {
                _queue.Enqueue(value);
                if (_queue.Count == 1)
                    _mutex.Set();
            }
        }

        public void OnError(Exception error)
        {
            CheckOnDispoce();

            throw error;
        }

        public void OnCompleted()
        {
            CheckOnDispoce();

            lock (_lockThis)
            {
                _isComplete = true;
                _mutex.Set();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            CheckOnDispoce();

            while (true)
            {
                while (!_isComplete && _queue.Count == 0)
                {
                    _mutex.WaitOne();
                    if (!_isComplete)
                        _mutex.Reset();
                }

                if (_isComplete)
                {
                    lock (_lockThis)
                    {
                        while (_queue.Count != 0)
                        {
                            var item = _queue.Dequeue();
                            if (item == null) throw new NullReferenceException("QueueItem is null");
                            yield return item;
                        }

                        yield break;
                    }
                }

                lock (_lockThis)
                {
                    var i = _queue.Dequeue();
                    if (i == null) throw new NullReferenceException("QueueItem is null");

                    yield return i;
                }
            }
        }

        public void Dispose()
        {
            if (_isDispoced) return;

            _isDispoced = true;
            _mutex.Dispose();

            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            CheckOnDispoce();

            return GetEnumerator();
        }

        private void CheckOnDispoce()
        {
            if (_isDispoced)
                throw new ObjectDisposedException("Object is already dispoced");
        }

        ~ProducerConsumerQueue()
        {
            _mutex.Dispose();
        }
    }


}
