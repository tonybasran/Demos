using Moq;
using Xunit.Abstractions;

namespace PollyDemo
{
    public static class MockJobExtensions
    {
        public static void SetupJobSuccessAfterForthAttempt(this Mock<IJob> mockJob, ITestOutputHelper testOutputHelper)
        {
            mockJob.SetupSequence(s => s.DoWork())
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Throws<DoWorkException>()
                .Returns(() =>
                {
                    testOutputHelper.WriteLine(JobConstants.CompletedJobMessage);
                    return true;
                });
        }
    }
}