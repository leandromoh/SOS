﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SOS.Administration.Api.Controllers.Interfaces
{
    /// <summary>
    ///     Harvest observations job controller
    /// </summary>
    public interface IHarvestObservationJobController
    {
        /// <summary>
        ///     Run observations harvest for the specified data providers.
        /// </summary>
        /// <param name="dataProviderIdOrIdentifiers"></param>
        /// <returns></returns>
        Task<IActionResult> RunObservationsHarvestJob([FromQuery] List<string> dataProviderIdOrIdentifiers);

        /// <summary>
        ///     Add daily harvest for the specified data providers.
        /// </summary>
        /// <param name="dataProviderIdOrIdentifiers"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        Task<IActionResult> AddObservationsHarvestJob([FromQuery] List<string> dataProviderIdOrIdentifiers,
            [FromQuery] int hour, [FromQuery] int minute);

    }
}