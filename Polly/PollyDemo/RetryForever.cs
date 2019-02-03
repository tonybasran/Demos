using System;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace PollyDemo
{
    public class RetryForever
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Mock<IJob> _mockJob;
        private readonly Policy _policy;

        public RetryForever(ITestOutputHelper testOutputHelper)
        {
            _mockJob = new Mock<IJob>();
            _mockJob.SetupJobSuccessAfterForthAttempt(testOutputHelper);
            _testOutputHelper = testOutputHelper;
            _policy = Policy.Handle<DoWorkException>()
                .RetryForever((exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int) failureCount);
                });
        }

        [Fact]
        public void CustomRetryForever_JobCompletesSuccessfullyAfterForthAttempt()
        {
            var jobResult = false;
            var jobCompletedSuccessfully = false;
            var failureCount = 0;

            while (jobCompletedSuccessfully == false)
            {
                try
                {
                    jobResult =  _mockJob.Object.DoWork();
                    jobCompletedSuccessfully = true;
                }
                catch (DoWorkException)
                {
                    failureCount++;
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, failureCount);
                }
            }

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
        }

        [Fact]
        public void PollyRetryForever_JobCompletesSuccessfullyAfterForthAttempt()
        {
            var jobResult = _policy.Execute(() =>  _mockJob.Object.DoWork());

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
        }

        [Fact]
        public void UnmatchedException_ForthExceptionIsNotDoWorkException_ExceptionIsThrown()
        {
            _mockJob.SetupSequence(s => s.DoWork())
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<Exception>();

            var jobResult = false;
            Assert.Throws<Exception>(() =>
            {
                jobResult = _policy.Execute(() => _mockJob.Object.DoWork());
            });
                
            Assert.False(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(4));
        }
    }
}
