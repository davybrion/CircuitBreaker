using System;
using System.Timers;

namespace CircuitBreaker
{
	public abstract class CircuitBreakerState
	{
		protected readonly CircuitBreaker circuitBreaker;

		protected CircuitBreakerState(CircuitBreaker circuitBreaker)
		{
			this.circuitBreaker = circuitBreaker;
		}

		public virtual void ProtectedCodeIsAboutToBeCalled() { }
		public virtual void ProtectedCodeHasBeenCalled() { }
		public virtual void ActUponException(Exception e) { circuitBreaker.IncreaseFailureCount(); }
	}

	public class ClosedState : CircuitBreakerState
	{
		public ClosedState(CircuitBreaker circuitBreaker) : base(circuitBreaker)
		{
			circuitBreaker.ResetFailureCount();
		}

		public override void ActUponException(Exception e)
		{
			base.ActUponException(e);
			if (circuitBreaker.ThresholdReached()) circuitBreaker.MoveToOpenState();
		}
	}

	public class OpenState : CircuitBreakerState
	{
		private readonly Timer timer;

		public OpenState(CircuitBreaker circuitBreaker) : base(circuitBreaker)
		{
			timer = new Timer(circuitBreaker.Timeout.TotalMilliseconds);
			timer.Elapsed += TimeoutHasBeenReached;
			timer.AutoReset = false;
			timer.Start();
		}

		private void TimeoutHasBeenReached(object sender, ElapsedEventArgs e)
		{
			circuitBreaker.MoveToHalfOpenState();
		}

		public override void ProtectedCodeIsAboutToBeCalled()
		{
			base.ProtectedCodeIsAboutToBeCalled();
			throw new OpenCircuitException();
		}
	}

	public class HalfOpenState : CircuitBreakerState
	{
		public HalfOpenState(CircuitBreaker circuitBreaker) : base(circuitBreaker) {}

		public override void ActUponException(Exception e)
		{
			base.ActUponException(e);
			circuitBreaker.MoveToOpenState();
		}

		public override void ProtectedCodeHasBeenCalled()
		{
			base.ProtectedCodeHasBeenCalled();
			circuitBreaker.MoveToClosedState();
		}
	}
}