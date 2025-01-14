﻿using System.Threading.Tasks;
using Hangfire;

namespace SOS.Lib.Jobs.Process
{
    public interface IActivateInstanceJob
    {
        /// <summary>
        ///     Activate passed instance
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        [Queue("high")]
        Task<bool> RunAsync(byte instance);
    }
}