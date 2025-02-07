using System;
using System.Threading;

namespace MetricsDataSource_1.Caches
{
    internal sealed class RefCountCache<T> : IDisposable where T : class, IDisposable
    {
        private static readonly TimeSpan ExpireTime = TimeSpan.FromMinutes(1);

        private readonly object _lock = new object();
        private readonly Func<T> _factory;
        private readonly Timer _timer;

        private int _refCount = 0;
        private T _value = null;
        private bool _isDisposed = false;

        public RefCountCache(Func<T> factory)
        {
            _factory = factory;
            _timer = new Timer(DisposeValue, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _timer.Dispose();
            DisposeValue(null);
        }

        public sealed class Handle : IDisposable
        {
            public T Value => _cache._value;

            private readonly RefCountCache<T> _cache;

            public Handle(RefCountCache<T> cache)
            {
                _cache = cache;
                _cache.Use();
            }

            public void Dispose()
            {
                _cache?.Release();
            }
        }

        public Handle GetHandle()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RefCountCache<T>));

            return new Handle(this);
        }

        private void Use()
        {
            lock (_lock)
            {
                _refCount++;
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                if (_value is null)
                {
                    _value = _factory();
                }
            }
        }

        private void Release()
        {
            lock ( _lock)
            {
                _refCount--;
                if (_refCount != 0)
                    return;
                _timer.Change(ExpireTime, Timeout.InfiniteTimeSpan);
            }
        }

        private void DisposeValue(object state)
        {
            try
            {
                if (_isDisposed)
                    return;

                lock (_lock)
                {
                    _value?.Dispose();
                    _value = null;
                }
            }
            catch
            {
                // Prevent crashing
            }
        }
    }
}
