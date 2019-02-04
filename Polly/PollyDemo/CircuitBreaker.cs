using System;
using System.Threading;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using Xunit;
using Xunit.Abstractions;

namespace PollyDemo
{
    public class CircuitBreaker
    {
        private readonly Mock<IJob> _mockJob;
          
        public CircuitBreaker()
        {
            _mockJob = new Mock<IJob>();
        }

        [Fact]
        public void CircuitBreakerPolicy_TwoHandledExceptionsBeforeBreakingWithThreeSecondWait()
        {
            _mockJob.SetupSequence(s => s.DoWork())
                .Returns(true)
                .Returns(true)
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Returns(true);

            var policy = Policy.Handle<DoWorkException>()
                .CircuitBreaker(2, TimeSpan.FromSeconds(3));

            // Circuit closed
            Assert.Equal(CircuitState.Closed, policy.CircuitState);
            Assert.True(policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.True(policy.Execute(() => _mockJob.Object.DoWork()));

            Assert.Throws<DoWorkException>(() => policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.Throws<DoWorkException>(() => policy.Execute(() => _mockJob.Object.DoWork()));

            Assert.Equal(CircuitState.Open, policy.CircuitState);
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));

            Thread.Sleep(TimeSpan.FromSeconds(3));

            Assert.Equal(CircuitState.HalfOpen, policy.CircuitState);
            Assert.Throws<DoWorkException>(() => policy.Execute(() => _mockJob.Object.DoWork()));

            Assert.Equal(CircuitState.Open, policy.CircuitState);
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));
            Assert.Throws<BrokenCircuitException>(() => policy.Execute(() => _mockJob.Object.DoWork()));

            Thread.Sleep(TimeSpan.FromSeconds(3));

            Assert.Equal(CircuitState.HalfOpen, policy.CircuitState);
            Assert.True(policy.Execute(() => _mockJob.Object.DoWork()));

            Assert.Equal(CircuitState.Closed, policy.CircuitState);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(6));
        }
    }
}