﻿namespace SOS.Lib.Models.Search.Result
{
    /// <summary>
    ///     Result returned year month aggregation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class YearMonthCountResult : YearCountResult
    {
        /// <summary>
        ///     Month
        /// </summary>
        public int Month { get; set; }
    }
}