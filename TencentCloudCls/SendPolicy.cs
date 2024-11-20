using System;

namespace TencentCloudCls
{
    public struct SendPolicy
    {
        public static readonly SendPolicy Immediate = new()
        {
            MaxRetry = 3,
        };

        public static readonly SendPolicy SmallBatch = new()
        {
            MaxBatchSize = 1 * 1024 * 1024,
            MaxBatchCount = 4096,
            FlushInterval = TimeSpan.FromMilliseconds(100),
            MaxRetry = 3,
            Worker = 10,
        };

        public static readonly SendPolicy LargeBatch = new()
        {
            MaxBatchSize = 4 * 1024 * 1024,
            MaxBatchCount = 1024,
            FlushInterval = TimeSpan.FromMilliseconds(5000),
            MaxRetry = 3,
            Worker = 10,
        };

        public static readonly SendPolicy Default = SmallBatch;

        // Upload all logs when cached log's size(in byte) exceed {MaxBatchSize} per topic.
        // MaxBatchSize = 0 means no batch.
        public ulong MaxBatchSize;

        // Upload all logs when cached logs exceed {MaxBatchCount} per topic.
        // MaxBatchCount = 0 means no batch.
        public ulong MaxBatchCount;

        // Upload all cached logs every {FlushInterval} per topic
        public TimeSpan FlushInterval;

        // Max upload retry before reporting error.
        public uint MaxRetry;

        // Number of workers to upload log in background.
        public uint Worker;

        public bool EnableBatch => MaxBatchSize > 0 && MaxBatchCount > 0 && Worker > 0;

        public void Validate()
        {
            // disable batch
            if (MaxBatchSize + MaxBatchCount + Worker == 0)
            {
                return;
            }

            if (MaxBatchCount is 0 or > 40960)
            {
                throw new ArgumentException("MaxBatchCount should be in range [0, 40960]");
            }

            if (MaxBatchSize is 0 or > 5 * 1024 * 1024)
            {
                throw new ArgumentException($"MaxBatchSize should be in range [0, {5 * 1024 * 1024}]");
            }

            if (Worker == 0)
            {
                throw new ArgumentException("Worker should be greater than 0");
            }
        }

        public struct Stat
        {
            public ulong BatchSize;
            public ulong BatchCount;
            public uint Retry;
            public DateTime LastUpload;
        }
    }
}