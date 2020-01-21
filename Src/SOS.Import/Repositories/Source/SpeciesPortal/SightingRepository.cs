﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SOS.Import.Entities;
using SOS.Import.Services.Interfaces;

namespace SOS.Import.Repositories.Source.SpeciesPortal
{ 
    public class SightingRepository : BaseRepository<SightingRepository>, Interfaces.ISightingRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SpeciesPortalDataService"></param>
        /// <param name="logger"></param>
        public SightingRepository(ISpeciesPortalDataService SpeciesPortalDataService, ILogger<SightingRepository> logger) : base(SpeciesPortalDataService, logger)
        {
           
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SightingEntity>> GetChunkAsync(int startId, int maxRows)
        {
            try
            {
                var query = @"
                SELECT DISTINCT
                    s.ActivityId,
					s.BiotopeId,
					sdb.[Description] AS BiotopeDescription,
                    ssci.Label AS CollectionID,
	                scp.Comment,
	                s.EndDate,
	                s.EndTime,
	                s.GenderId,
                    s.HasImages,
	                s.HiddenByProvider,
	                s.Id, 
	                ssc.Label,
	                s.[Length],
                    s.MaxDepth,
					s.MaxHeight,
					s.MinDepth,
					s.MinHeight,
	                s.NotPresent,
	                s.NotRecovered,
                    s.OwnerOrganizationId,
                    msi.PortalId AS MigrateSightingPortalId,
                    msi.obsid AS MigrateSightingObsId,
	                s.ProtectedBySystem,
	                s.Quantity,
					s.QuantityOfSubstrate,
                    s.RegisterDate,
	                CASE 
						WHEN p.Id IS NULL THEN null
						ELSE p.FirstName + ' ' + p.LastName 
					END AS RightsHolder,
	                s.SiteId,
	                s.StageId,
	                s.StartDate,
	                s.StartTime,
					s.SubstrateId,
					sds.[Description] AS SubstrateDescription,
					sdss.[Description] AS SubstrateSpeciesDescription,
					s.SubstrateSpeciesId,
	                s.TaxonId,
	                s.UnsureDetermination,
	                s.Unspontaneous,
	                s.UnitId,
	                sb.URL,
                    s.ValidationStatusId,
	                s.[Weight]
                FROM
	                SearchableSightings s WITH(NOLOCK)
					INNER JOIN Sighting si ON s.SightingId = si.Id
	                INNER JOIN SightingState ss ON s.SightingId = ss.SightingId
	                LEFT JOIN SightingCommentPublic scp ON s.SightingId = scp.SightingId
	                LEFT JOIN SightingSpeciesCollectionItem ssc ON s.SightingId = ssc.SightingId
	                LEFT JOIN SightingBarcode sb ON s.SightingId = sb.SightingId
                    LEFT JOIN [User] u ON s.OwnerUserId = u.Id 
	                LEFT JOIN Person p ON u.PersonId = p.Id
                    LEFT JOIN SightingSpeciesCollectionItem ssci ON s.SightingId = ssci.SightingId
                    LEFT JOIN MigrateSightingid msi ON s.SightingId = msi.Id
					LEFT JOIN SightingDescription sdb ON si.SightingBiotopeDescriptionId = sdb.Id 
					LEFT JOIN SightingDescription sds ON si.SightingSubstrateDescriptionId = sds.Id 
					LEFT JOIN SightingDescription sdss ON si.SightingSubstrateSpeciesDescriptionId = sdss.Id
                WHERE
	                s.Id BETWEEN @StartId AND @EndId
	                AND s.TaxonId IS NOT NULL
	                AND s.SightingTypeId IN(0, 3) 
	                AND s.HiddenByProvider IS NULL
	                AND s.ValidationStatusId NOT IN(50)
	                AND s.SightingTypeSearchGroupId & 33 > 0
	                AND ss.IsActive = 1
	                AND ss.SightingStateTypeId = 30--Published
	                AND(ss.EndDate IS NULL OR ss.EndDate > GETDATE())";

                return await QueryAsync<SightingEntity>(query, new { StartId = startId, EndId = startId + maxRows -1 });
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting sightings");

                return null;
            }
        }

		/// <summary>
		/// Get sightings for the specified sighting ids. Used for testing purpose for retrieving specific sightings from Artportalen.
		/// This method should be the same as GetChunkAsync(int startId, int maxRows), with
		/// the difference that this method uses a list of sighting ids instead of (startId, maxRows).
		/// </summary>
		public async Task<IEnumerable<SightingEntity>> GetChunkAsync(IEnumerable<int> sightingIds)
        {
			try
			{
				var query = @"
                SELECT DISTINCT
                    s.ActivityId,
					s.BiotopeId,
					sdb.[Description] AS BiotopeDescription,
                    ssci.Label AS CollectionID,
	                scp.Comment,
					si.ControlingOrganisationId,
	                s.EndDate,
	                s.EndTime,
	                s.GenderId,
                    s.HasImages,
	                s.HiddenByProvider,
	                s.Id, 
	                ssc.Label,
	                s.[Length],
                    s.MaxDepth,
					s.MaxHeight,
					s.MinDepth,
					s.MinHeight,
	                s.NotPresent,
	                s.NotRecovered,
                    s.OwnerOrganizationId,
                    msi.PortalId AS MigrateSightingPortalId,
                    msi.obsid AS MigrateSightingObsId,
	                s.ProtectedBySystem,
	                s.Quantity,
					s.QuantityOfSubstrate,
                    s.RegisterDate,
	                CASE 
						WHEN p.Id IS NULL THEN null
						ELSE p.FirstName + ' ' + p.LastName 
					END AS RightsHolder,
	                s.SiteId,
	                s.StageId,
	                s.StartDate,
	                s.StartTime,
					s.SubstrateId,
					sds.[Description] AS SubstrateDescription,
					sdss.[Description] AS SubstrateSpeciesDescription,
					s.SubstrateSpeciesId,
	                s.TaxonId,
	                s.UnsureDetermination,
	                s.Unspontaneous,
	                s.UnitId,
	                sb.URL,
                    s.ValidationStatusId,
	                s.[Weight]
                FROM
	                SearchableSightings s WITH(NOLOCK)
					INNER JOIN Sighting si ON s.SightingId = si.Id
	                INNER JOIN SightingState ss ON s.SightingId = ss.SightingId
	                LEFT JOIN SightingCommentPublic scp ON s.SightingId = scp.SightingId
	                LEFT JOIN SightingSpeciesCollectionItem ssc ON s.SightingId = ssc.SightingId
	                LEFT JOIN SightingBarcode sb ON s.SightingId = sb.SightingId
                    LEFT JOIN [User] u ON s.OwnerUserId = u.Id 
	                LEFT JOIN Person p ON u.PersonId = p.Id
                    LEFT JOIN SightingSpeciesCollectionItem ssci ON s.SightingId = ssci.SightingId
                    LEFT JOIN MigrateSightingid msi ON s.SightingId = msi.Id
					LEFT JOIN SightingDescription sdb ON si.SightingBiotopeDescriptionId = sdb.Id 
					LEFT JOIN SightingDescription sds ON si.SightingSubstrateDescriptionId = sds.Id 
					LEFT JOIN SightingDescription sdss ON si.SightingSubstrateSpeciesDescriptionId = sdss.Id
                WHERE
	                s.Id in @ids
	                AND s.TaxonId IS NOT NULL
	                AND s.SightingTypeId IN(0, 3) 
	                AND s.HiddenByProvider IS NULL
	                AND s.ValidationStatusId NOT IN(50)
	                AND s.SightingTypeSearchGroupId & 33 > 0
	                AND ss.IsActive = 1
	                AND ss.SightingStateTypeId = 30--Published
	                AND(ss.EndDate IS NULL OR ss.EndDate > GETDATE())";

				return await QueryAsync<SightingEntity>(query, new { ids = sightingIds});
			}
			catch (Exception e)
			{
				Logger.LogError(e, "Error getting sightings");

				return null;
			}
		}

        /// <inheritdoc />
        public async Task<Tuple<int, int>> GetIdSpanAsync()
        {
            try
            {
                const string query = @"
                SELECT 
                    MIN(Id) AS Item1,
                    MAX(Id) AS Item2
		        FROM 
		            SearchableSightings s";

               return (await QueryAsync<Tuple<int, int>>(query)).FirstOrDefault();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting min and max id");

                return null;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<(int SightingId, int ProjectId)>> GetProjectIdsAsync()
        {
            try
            {
                const string query = @"
                SELECT 
                    ps.SightingId AS SightingId,
	                ps.ProjectId AS ProjectId
                FROM 
	                ProjectSighting ps
                ORDER BY ps.ProjectId DESC";

                return await QueryAsync<(int SightingId, int ProjectId)>(query);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error getting sighting/project connections");
                return null;
            }
        }
    }
}