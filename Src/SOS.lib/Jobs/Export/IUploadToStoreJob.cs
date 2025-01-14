﻿using System.ComponentModel;
using System.Threading.Tasks;
using Hangfire;

namespace SOS.Lib.Jobs.Export
{
    /// <summary>
    ///     Interface for Upload file to blob storage job
    /// </summary>
    public interface IUploadToStoreJob
    {
        /// <summary>
        /// Upload file to blob storage
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="blobStorageContainer"></param>
        /// <param name="deleteSourceOnSuccess"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [DisplayName("Upload file to Azure Blob Storage [Src=\"{0}\"]")]
        [Queue("medium")]
        Task<bool> RunAsync(string sourcePath, string blobStorageContainer, bool deleteSourceOnSuccess,
            IJobCancellationToken cancellationToken);
    }
}