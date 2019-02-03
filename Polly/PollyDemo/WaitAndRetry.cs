using System;
using System.Diagnostics;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace PollyDemo
{
    public class WaitAndRetry
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IJob> _mockJob;
        private readonly Stopwatch _stopWatch;

        public WaitAndRetry(ITestOutputHelper testOutputHelper)
        {
            _mockJob = new Mock<IJob>();
            _mockJob.SetupJobSuccessAfterForthAttempt(testOutputHelper);
            _testOutputHelper = testOutputHelper;
            _stopWatch = new Stopwatch();
        }

        [Fact]
        public void WaitAndRetry_2Retries_ThrowsDoWorkException()
        {
            var jobResult = false;
            _stopWatch.Start();

            Assert.Throws<DoWorkException>(() =>
            {
                jobResult = Policy.Handle<DoWorkException>()
                    .WaitAndRetry(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                    },
                    (exception, timeSpan, failureCount, context) =>
                    {
                        _testOutputHelper.WriteLine(JobConstants.JobFailedWithWaitMessage, failureCount, timeSpan.TotalSeconds);
                    })
                    .Execute(() => _mockJob.Object.DoWork());
            });

            _stopWatch.Stop();
            Assert.False(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(3));
            // 1 + 2 = 3
            Assert.True(_stopWatch.Elapsed.TotalSeconds > 3);
        }

        [Fact]
        public void WaitAndRetry_3Retries_DoesNotThrowDoWorkException()
        {
            _stopWatch.Start();

            var jobResult = Policy.Handle<DoWorkException>()
                .WaitAndRetry(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(3),
                    },
                    (exception, timeSpan, failureCount, context) =>
                    {
                        _testOutputHelper.WriteLine(JobConstants.JobFailedWithWaitMessage, failureCount, timeSpan.TotalSeconds);
                    })
                .Execute(() => _mockJob.Object.DoWork());

            _stopWatch.Stop();
            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
            // 1 + 2 + 3 = 6
            Assert.True(_stopWatch.Elapsed.TotalSeconds > 6);
        }

        [Fact]
        public void WaitAndRetry_WithSleepDurationProviderDelegate_DoesNotThrowDoWorkException()
        {
            _stopWatch.Start();

            var jobResult = Policy.Handle<DoWorkException>()
                .WaitAndRetry(3, retryCount => TimeSpan.FromSeconds(retryCount * retryCount),
                    (exception, timeSpan, failureCount, context) =>
                    {
                        _testOutputHelper.WriteLine(JobConstants.JobFailedWithWaitMessage, failureCount, timeSpan.TotalSeconds);
                    })
                .Execute(() => _mockJob.Object.DoWork());

            _stopWatch.Stop();
            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
            // (1*1) + (2*2) + (3*3) = 14
            Assert.True(_stopWatch.Elapsed.TotalSeconds > 14);
        }
    }
}