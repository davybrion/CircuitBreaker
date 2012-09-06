using System;

namespace CircuitBreaker
{
	public class OpenCircuitException : Exception
	{
		public OpenCircuitException() : base("The protected code can not be called while the circuit is open") {}
	}
}