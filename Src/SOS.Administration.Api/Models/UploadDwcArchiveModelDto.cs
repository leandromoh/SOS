﻿using Microsoft.AspNetCore.Http;

namespace SOS.Administration.Api.Models
{
    /// <summary>
    /// DTO for handling upload of a DwC-A file.
    /// </summary>
    public class UploadDwcArchiveModelDto
    {
        /// <summary>
        /// Data provider id.
        /// </summary>
        public int DataProviderId { get; set; }

        /// <summary>
        /// DwC-A file.
        /// </summary>
        public IFormFile DwcaFile { get; set; }
    }
}