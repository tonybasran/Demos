using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Moq;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace PollyDemo
{
    public class AdditionalFeatures
    {
        protected internal const string JobFailedMessage = "Job failed";
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Fixture fixture;
        private readonly Mock<IJob> _mockJob;

        public AdditionalFeatures(ITestOutputHelper testOutputHelper)
        {
            fixture = new Fixture();
            _mockJob = new Mock<IJob>();
            _mockJob.SetupJobSuccessAfterForthAttempt(testOutputHelper);
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void HandlingMultipleExceptions_HandleDoWorkExceptionAndExceptionTypes()
        {
            _mockJob.SetupSequence(s => s.DoWork())
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<Exception>()
                .Returns(true);

            var jobResult = Policy.Handle<DoWorkException>()
                .Or<Exception>()
                .RetryForever((exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                })
                .Execute(() => _mockJob.Object.DoWork());

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(5));
        }

        [Fact]
        public async Task AsyncMethodSupport()
        {
            _mockJob.SetupSequence(s => s.DoWorkAsync())
                .ThrowsAsync(new DoWorkException())
                .ThrowsAsync(new DoWorkException())
                .ThrowsAsync(new DoWorkException())
                .ThrowsAsync(new DoWorkException())
                .ReturnsAsync(true);

            var jobResult = await Policy.Handle<DoWorkException>()
                .Or<Exception>()
                .RetryForeverAsync((exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                })
                .ExecuteAsync(() => _mockJob.Object.DoWorkAsync());

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWorkAsync(), Times.Exactly(5));
        }

        [Fact]
        public void AdditionalExceptionConfiguration_DoWorkExceptionMessageIsNotJobFailed_DoWorkExceptionThrown()
        {
            _mockJob.SetupSequence(s => s.DoWork())
                .Throws(new DoWorkException(JobFailedMessage))
                .Throws(new DoWorkException())
                .Returns(true);

            var jobResult = false;

            Assert.Throws<DoWorkException>(() =>
            {
                jobResult = Policy.Handle<DoWorkException>(exception => exception.Message == JobFailedMessage)
                .RetryForever((exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                })
                .Execute(() => _mockJob.Object.DoWork());
            });

            Assert.False(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(2));
        }

        [Fact]
        public void AdditionalExceptionConfiguration_DoWorkExceptionMessageIsJobFailedOrNull_DoesNotThrowDoWorkException()
        {
            var doWorkExceptionMessage = fixture.Create<string>();
            var exceptionsMessages = new[] {JobFailedMessage, doWorkExceptionMessage};
            _mockJob.SetupSequence(s => s.DoWork())
                .Throws(new DoWorkException(JobFailedMessage))
                .Throws(new DoWorkException(doWorkExceptionMessage))
                .Returns(true);

            var jobResult = Policy.Handle<DoWorkException>(exception => exceptionsMessages.Contains(exception.Message))
                .RetryForever((exception, failureCount) =>
                {
                    _testOutputHelper.WriteLine(JobConstants.JobFailedMessage, (int)failureCount);
                })
                .Execute(() => _mockJob.Object.DoWork());

            Assert.True(jobResult);
            _mockJob.Verify(v => v.DoWork(), Times.Exactly(3));
        }
    }
}