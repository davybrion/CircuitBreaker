using System;
using System.Threading;

using NUnit.Framework;

namespace CircuitBreaker.Tests
{
	[TestFixture]
	public class CircuitBreakerTests
	{
		private static void DummyCall() {}

		private static void AssertThatExceptionIsThrown<T>(Action code) where T : Exception
		{
			try
			{
				code();
			}
			catch (T)
			{
				return;
			}

			Assert.Fail("Expected exception of type {0} was not thrown", typeof(T).FullName);
		}

		private static void CallXAmountOfTimes(Action codeToCall, int timesToCall)
		{
			for (int i = 0; i < timesToCall; i++)
			{
				codeToCall();
			}
		}

		[Test]
		public void AttemptCallCallsProtectedCode()
		{
			bool protectedCodeWasCalled = false;
			Action protectedCode = () => protectedCodeWasCalled = true;

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			circuitBreaker.AttemptCall(protectedCode);
			Assert.That(protectedCodeWasCalled);
		}

		[Test]
		public void FailuresIsNotIncreasedWhenProtectedCodeSucceeds()
		{
			Action protectedCode = () => { return; };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			circuitBreaker.AttemptCall(protectedCode);
			Assert.AreEqual(0, circuitBreaker.Failures);
		}

		[Test]
		public void ConstructorWithInvalidThresholdThrowsException()
		{
			AssertThatExceptionIsThrown<ArgumentOutOfRangeException>(
				() => new CircuitBreaker(0, TimeSpan.FromMinutes(5)));
			AssertThatExceptionIsThrown<ArgumentOutOfRangeException>(
				() => new CircuitBreaker(-1, TimeSpan.FromMinutes(5)));
		}

		[Test]
		public void ConstructorWithInvalidTimeoutThrowsException()
		{
			AssertThatExceptionIsThrown<ArgumentOutOfRangeException>(
				() => new CircuitBreaker(10, TimeSpan.Zero));
			AssertThatExceptionIsThrown<ArgumentOutOfRangeException>(
				() => new CircuitBreaker(10, TimeSpan.FromMilliseconds(-1)));
		}

		[Test]
		public void FailuresIncreasesWhenProtectedCodeFails()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			Assert.AreEqual(0, circuitBreaker.Failures);
			AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode));
			Assert.AreEqual(1, circuitBreaker.Failures);
		}

		[Test]
		public void NewCircuitBreakerIsClosed()
		{
			var circuitBreaker = new CircuitBreaker(5, TimeSpan.FromMinutes(5));
			Assert.That(circuitBreaker.IsClosed);
		}

		[Test]
		public void OpensWhenThresholdIsReached()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode)), 10);
			Assert.That(circuitBreaker.IsOpen);
		}

		[Test]
		public void TestConstructorWithValidArguments()
		{
			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			Assert.AreEqual(10, circuitBreaker.Threshold);
			Assert.AreEqual(TimeSpan.FromMinutes(5), circuitBreaker.Timeout);
		}

		[Test]
		public void ThrowsOpenCircuitExceptionWhenCallIsAttemptedIfCircuitBreakerIsOpen()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMinutes(5));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode)), 10);
			AssertThatExceptionIsThrown<OpenCircuitException>(() => circuitBreaker.AttemptCall(protectedCode));
		}

		[Test]
		public void SwitchesToHalfOpenWhenTimeOutIsReachedAfterOpening()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode)), 10);
			Thread.Sleep(100);
			Assert.That(circuitBreaker.IsHalfOpen);
		}

		[Test]
		public void OpensIfExceptionIsThrownInProtectedCodeWhenInHalfOpenState()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode)), 10);
			Thread.Sleep(100);
			AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode));
			Assert.That(circuitBreaker.IsOpen);
		}

		[Test]
		public void ClosesIfProtectedCodeSucceedsInHalfOpenState()
		{
			var stub = new Stub(10);
			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(stub.DoStuff)), 10);
			Thread.Sleep(100);
			circuitBreaker.AttemptCall(stub.DoStuff);
			Assert.That(circuitBreaker.IsClosed);
		}

		[Test]
		public void FailuresIsResetWhenCircuitBreakerCloses()
		{
			var stub = new Stub(10);
			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(stub.DoStuff)), 10);
			Assert.AreEqual(10, circuitBreaker.Failures);
			Thread.Sleep(100);
			circuitBreaker.AttemptCall(stub.DoStuff);
			Assert.AreEqual(0, circuitBreaker.Failures);
		}

		[Test]
		public void CanCloseCircuitBreaker()
		{
			Action protectedCode = () => { throw new ApplicationException("blah"); };

			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			CallXAmountOfTimes(() => AssertThatExceptionIsThrown<ApplicationException>(() => circuitBreaker.AttemptCall(protectedCode)), 10);
			Assert.That(circuitBreaker.IsOpen);
			circuitBreaker.Close();
			Assert.That(circuitBreaker.IsClosed);
		}

		[Test]
		public void CanOpenCircuitBreaker()
		{
			var circuitBreaker = new CircuitBreaker(10, TimeSpan.FromMilliseconds(50));
			Assert.That(circuitBreaker.IsClosed);
			circuitBreaker.Open();
			Assert.That(circuitBreaker.IsOpen);
		}
	}

	class Stub
	{
		private readonly int timesToFail;
		private int counter;

		public Stub(int timesToFail)
		{
			counter = 0;
			this.timesToFail = timesToFail;
		}

		public void DoStuff()
		{
			if (++counter <= timesToFail)
			{
				throw new ApplicationException("blah");
			}
		}
	}
}