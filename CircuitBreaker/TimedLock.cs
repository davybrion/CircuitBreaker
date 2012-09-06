using System;
using System.Threading;

namespace CircuitBreaker
{
	public struct TimedLock : IDisposable
	{
		private readonly object target;

		private TimedLock(object o)
		{
			target = o;
		}

		public void Dispose()
		{
			Monitor.Exit(target);
		}

		public static TimedLock Lock(object o)
		{
			return Lock(o, TimeSpan.FromSeconds(5));
		}

		public static TimedLock Lock(object o, TimeSpan timeout)
		{
			return Lock(o, timeout.Milliseconds);
		}

		public static TimedLock Lock(object o, int milliSeconds)
		{
			var timedLock = new TimedLock(o);

			if (!Monitor.TryEnter(o, milliSeconds))
			{
				throw new LockTimeoutException();
			}

			return timedLock;
		}
	}

	public class LockTimeoutException : ApplicationException
	{
		public LockTimeoutException() : base("Timeout waiting for lock") {}
	}
}