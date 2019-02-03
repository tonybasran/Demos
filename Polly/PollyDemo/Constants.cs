namespace PollyDemo
{
    public static class JobConstants
    {
        public const string CompletedJobMessage = "Job completed successfully";
        public const string JobFailedMessage = "Job failed. Number of times: {0}";
        public const string JobFailedWithWaitMessage = "Job failed. Number of times: {0}. Waiting {1} second/s to retry";
    }
}