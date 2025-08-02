using System;
using System.Threading;
using System.Threading.Tasks;

namespace EhViewer
{
    public class Limit
    {
        // SemaphoreSlim 的构造函数需要两个参数：
        // initialCount: 初始的可用并发数
        // maxCount: 最大的可用并发数
        private readonly SemaphoreSlim _semaphore;

        public Limit(int max)
        {
            // 初始化时，允许 max 个并发同时进入
            _semaphore = new SemaphoreSlim(max, max);
        }

        public async Task Enter()
        {
            // 等待获取一个许可。如果许可数为0，则异步等待直到有许可可用。
            // 这是非阻塞的，并且是高效的。
            await _semaphore.WaitAsync();
        }

        public void Exit()
        {
            // 释放一个许可。这会增加许可计数，并可能唤醒一个或多个等待的任务。
            _semaphore.Release();
        }

        // 静态实例
        public static Limit g_limit = new Limit(5);

        // 示例：如何使用 with using 模式确保 Release 被调用
        public async Task<IDisposable> EnterWithDisposable()
        {
            await _semaphore.WaitAsync();
            return new Releaser(_semaphore);
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}