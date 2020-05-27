﻿using Microsoft.AspNetCore.Mvc;

namespace SOS.Administration.Api.Controllers.Interfaces
{
    /// <summary>
    /// Harvest observations job controller
    /// </summary>
    public interface IHarvestObservationJobController
    {
        /// <summary>
        /// Add daily harvest of sightings from clam/tree portal
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyClamPortalHarvestJob(int hour, int minute);

        /// <summary>
        /// Run clam/tree portal sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunClamPortalHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from KUL
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyKulHarvestJob(int hour, int minute);

        /// <summary>
        /// Run KUL sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunKulHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from MVM
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyMvmHarvestJob(int hour, int minute);

        /// <summary>
        /// Run MVM sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunMvmHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from NORS
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyNorsHarvestJob(int hour, int minute);

        /// <summary>
        /// Run NORS sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunNorsHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from SERS
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailySersHarvestJob(int hour, int minute);

        /// <summary>
        /// Run SERS sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunSersHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from SHARK
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailySharkHarvestJob(int hour, int minute);

        /// <summary>
        /// Run SHARK sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunSharkHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from species data portal
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyArtportalenHarvestJob(int hour, int minute);

        /// <summary>
        /// Run Artportalen sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunArtportalenHarvestJob();

        /// <summary>
        /// Add daily harvest of sightings from Virtual Herbarium
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <returns></returns>
        IActionResult AddDailyVirtualHerbariumHarvestJob(int hour, int minute);

        /// <summary>
        /// Run Virtual Herbarium sightings harvest
        /// </summary>
        /// <returns></returns>
        IActionResult RunVirtualHerbariumHarvestJob();
    }
}