using System;

namespace CircuitBreaker
{
	public class CircuitBreaker
	{
		private readonly object monitor = new object();
		private CircuitBreakerState state;

		public CircuitBreaker(int threshold, TimeSpan timeout)
		{
			if (threshold < 1)
			{
				throw new ArgumentOutOfRangeException("threshold", "Threshold should be greater than 0");
			}

			if (timeout.TotalMilliseconds < 1)
			{
				throw new ArgumentOutOfRangeException("timeout", "Timeout should be greater than 0");
			}

			Threshold = threshold;
			Timeout = timeout;
			MoveToClosedState();
		}

		public int Failures { get; private set; }
		public int Threshold { get; private set; }
		public TimeSpan Timeout { get; private set; }

		public bool IsClosed
		{
			get { return state is ClosedState; }
		}

		public bool IsOpen
		{
			get { return state is OpenState; }
		}

		public bool IsHalfOpen
		{
			get { return state is HalfOpenState; }
		}

		internal void MoveToClosedState()
		{
			state = new ClosedState(this);
		}

		internal void MoveToOpenState()
		{
			state = new OpenState(this);
		}

		internal void MoveToHalfOpenState()
		{
			state = new HalfOpenState(this);
		}

		internal void IncreaseFailureCount()
		{
			Failures++;
		}

		internal void ResetFailureCount()
		{
			Failures = 0;
		}

		public bool ThresholdReached()
		{
			return Failures >= Threshold;
		}

		public void AttemptCall(Action protectedCode)
		{
			using (TimedLock.Lock(monitor)) 
			{
				state.ProtectedCodeIsAboutToBeCalled();
			}

			try
			{
				protectedCode();
			}
			catch (Exception e)
			{
				using (TimedLock.Lock(monitor))
				{
					state.ActUponException(e);
				}
				throw;
			}

			using (TimedLock.Lock(monitor))
			{
				state.ProtectedCodeHasBeenCalled();
			}
		}

		public void Close()
		{
			using (TimedLock.Lock(monitor))
			{
				MoveToClosedState();
			}
		}

		public void Open()
		{
			using (TimedLock.Lock(monitor))
			{
				MoveToOpenState();
			}
		}
	}
}