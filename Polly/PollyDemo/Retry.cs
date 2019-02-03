using Moq;
using Polly;
using Polly.Retry;
using Xunit;
using Xunit.Abstractions;

namespace PollyDemo
{
    public class Retry
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IJob> _mockJob;

        public Retry(ITestOutputHelper testOutputHelper)
        {
            _mockJob = new Mock<IJob>();
            _mockJob.SetupJobSuccessAfterForthAttempt(testOutputHelper);
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void CustomRetry()
        {
            var jobResult = false;
            var jobCompletedSuccessfully = false;
            var failureCount = 0;
            var retryAttempts = 4;

            while (jobCompletedSuccessfully == false)
            {
                try
                {
                    jobResult = _mockJob.Object.DoWork();
                    jobCompletedSuccessfully = true;
                }
                catch (DoWorkException)
                {
                    failureCount++;
                    if (retryAttempts == failureCount)
                    {
                        throw;
                    }
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, failureCount);
                }
            }

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
        }

        [Fact]
        public void PollyRetry_2Retries_ThrowsDoWorkException()
        {
            var jobResult = false;

            Assert.Throws<DoWorkException>(() =>
                {
                    jobResult = Policy.Handle<DoWorkException>()
                        .Retry(2, (exception, failureCount) =>
                        {
                            _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                        })
                        .Execute(() => _mockJob.Object.DoWork());
                });

            Assert.False(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(3));
        }

        [Fact]
        public void PollyRetry_3Retries_DoesNotThrowDoWorkException()
        {
            var jobResult = Policy.Handle<DoWorkException>()
                .Retry(3, (exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                })
                .Execute(() => _mockJob.Object.DoWork());

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
        }
    }
}