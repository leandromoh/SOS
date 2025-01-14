﻿using System.Collections.Generic;
using DwC_A.Meta;

namespace DwC_A
{
    /// <summary>
    ///     Reads a file
    /// </summary>
    public interface IAsyncFileReader
    {
        /// <summary>
        ///     Fully qualified path to file
        /// </summary>
        string FileName { get; }

        /// <summary>
        ///     Collection of metadata for file
        /// </summary>
        IFileMetaData FileMetaData { get; }

        /// <summary>
        ///     Enumerable collection of data row objects
        /// </summary>
        IAsyncEnumerable<IRow> GetDataRowsAsync();

        /// <summary>
        ///     Enumerable collection of header row objects
        /// </summary>
        IAsyncEnumerable<IRow> GetHeaderRowsAsync();

        /// <summary>
        ///     Enumerable collection of all row objects including headers and data
        /// </summary>
        IAsyncEnumerable<IRow> GetRowsAsync();

        /// <summary>
        ///     Try to get field metadata.
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        FieldType TryGetFieldMetaData(string term);

        int GetIdIndex();

        int GetNumberOfRows();
    }
}