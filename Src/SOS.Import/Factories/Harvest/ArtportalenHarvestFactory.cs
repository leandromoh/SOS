﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using SOS.Import.Containers.Interfaces;
using SOS.Import.Entities.Artportalen;
using SOS.Import.Factories.Harvest.Interfaces;
using SOS.Import.Repositories.Source.Artportalen.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Extensions;
using SOS.Lib.Helpers.Interfaces;
using SOS.Lib.Models.Verbatim.Artportalen;

namespace SOS.Import.Factories.Harvest
{
    internal class ArtportalenHarvestFactory : HarvestBaseFactory, IHarvestFactory<SightingEntity[], ArtportalenObservationVerbatim>
    {
        private readonly IArtportalenMetadataContainer _artportalenMetadataContainer;

        private readonly IProjectRepository _projectRepository;
        private readonly ISightingRepository _sightingRepository;
        private readonly ISiteRepository _siteRepository;
        private readonly ISightingRelationRepository _sightingRelationRepository;
        private readonly ISpeciesCollectionItemRepository _speciesCollectionRepository;
        private readonly IAreaHelper _areaHelper;
        private readonly ConcurrentDictionary<int, Site> _sites;

        /// <summary>
        /// Cast sighting itemEntity to model .
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="personSightings"></param>
        /// <param name="sightingsProjects"></param>
        /// <returns></returns>
        private ArtportalenObservationVerbatim CastEntityToVerbatim(SightingEntity entity,
            IDictionary<int, PersonSighting> personSightings,
            IDictionary<int, Project[]> sightingsProjects)
        {
            if (entity == null)
            {
                return null;
            }

            if (_sites.TryGetValue(entity.SiteId.HasValue ? entity.SiteId.Value : -1, out var site))
            {
                // Try to set parent site name if empty
                if (site?.ParentSiteId != null && string.IsNullOrEmpty(site.ParentSiteName))
                {
                    if (_sites.TryGetValue(site.ParentSiteId.Value, out var parentSite))
                    {
                        site.ParentSiteName = parentSite.Name;
                    }
                }
            }

            var observation = new ArtportalenObservationVerbatim
            {
                Activity = entity.ActivityId.HasValue && _artportalenMetadataContainer.Activities.ContainsKey(entity.ActivityId.Value)
                    ? _artportalenMetadataContainer.Activities[entity.ActivityId.Value]
                    : null,
                Biotope = entity.BiotopeId.HasValue && _artportalenMetadataContainer.Biotopes.ContainsKey(entity.BiotopeId.Value)
                    ? _artportalenMetadataContainer.Biotopes[entity.BiotopeId.Value]
                    : null,
                BiotopeDescription = entity.BiotopeDescription,
                CollectionID = entity.CollectionID,
                Comment = entity.Comment,
                DiscoveryMethod = entity.DiscoveryMethodId.HasValue && _artportalenMetadataContainer.DiscoveryMethods.ContainsKey(entity.DiscoveryMethodId.Value)
                    ? _artportalenMetadataContainer.DiscoveryMethods[entity.DiscoveryMethodId.Value]
                    : null,
                DeterminationMethod = entity.DeterminationMethodId.HasValue && _artportalenMetadataContainer.DeterminationMethods.ContainsKey(entity.DeterminationMethodId.Value)
                    ? _artportalenMetadataContainer.DeterminationMethods[entity.DeterminationMethodId.Value]
                    : null,
                EditDate = entity.EditDate,
                EndDate = entity.EndDate,
                EndTime = entity.EndTime,
                Gender = entity.GenderId.HasValue && _artportalenMetadataContainer.Genders.ContainsKey(entity.GenderId.Value)
                    ? _artportalenMetadataContainer.Genders[entity.GenderId.Value]
                    : null,
                HasImages = entity.HasImages,
                FirstImageId = entity.FirstImageId,
                HasTriggeredValidationRules = entity.HasTriggeredValidationRules,
                HasAnyTriggeredValidationRuleWithWarning = entity.HasAnyTriggeredValidationRuleWithWarning,
                HiddenByProvider = entity.HiddenByProvider,
                Id = NextId,
                SightingId = entity.Id,
                OwnerOrganization =
                    entity.OwnerOrganizationId.HasValue &&
                    _artportalenMetadataContainer.Organizations.ContainsKey(entity.OwnerOrganizationId.Value)
                        ? _artportalenMetadataContainer.Organizations[entity.OwnerOrganizationId.Value]
                        : null,
                Label = entity.Label,
                Length = entity.Length,
                MaxDepth = entity.MaxDepth,
                MaxHeight = entity.MaxHeight,
                MigrateSightingObsId = entity.MigrateSightingObsId,
                MigrateSightingPortalId = entity.MigrateSightingPortalId,
                MinDepth = entity.MinDepth,
                MinHeight = entity.MinHeight,
                NoteOfInterest = entity.NoteOfInterest,
                HasUserComments = entity.HasUserComments,
                NotPresent = entity.NotPresent,
                NotRecovered = entity.NotRecovered,
                ProtectedBySystem = entity.ProtectedBySystem,
                Quantity = entity.Quantity,
                QuantityOfSubstrate = entity.QuantityOfSubstrate,
                ReportedDate = entity.RegisterDate,
                RightsHolder = entity.RightsHolder,
                Site = site,
                SightingSpeciesCollectionItemId = entity.SightingSpeciesCollectionItemId,
                Stage = entity.StageId.HasValue && _artportalenMetadataContainer.Stages.ContainsKey(entity.StageId.Value)
                    ? _artportalenMetadataContainer.Stages[entity.StageId.Value]
                    : null,
                StartDate = entity.StartDate,
                StartTime = entity.StartTime,
                Substrate = entity.SubstrateId.HasValue && _artportalenMetadataContainer.Substrates.ContainsKey(entity.SubstrateId.Value)
                    ? _artportalenMetadataContainer.Substrates[entity.SubstrateId.Value]
                    : null,
                SubstrateDescription = entity.SubstrateDescription,
                SubstrateSpeciesDescription = entity.SubstrateSpeciesDescription,
                SubstrateSpeciesId = entity.SubstrateSpeciesId,
                TaxonId = entity.TaxonId,
                Unit = entity.UnitId.HasValue && _artportalenMetadataContainer.Units.ContainsKey(entity.UnitId.Value)
                    ? _artportalenMetadataContainer.Units[entity.UnitId.Value]
                    : null,
                Unspontaneous = entity.Unspontaneous,
                UnsureDetermination = entity.UnsureDetermination,
                URL = entity.URL,
                ValidationStatus = _artportalenMetadataContainer.ValidationStatus.ContainsKey(entity.ValidationStatusId)
                    ? _artportalenMetadataContainer.ValidationStatus[entity.ValidationStatusId]
                    : null,
                Weight = entity.Weight,
                Projects = sightingsProjects?.ContainsKey(entity.Id) ?? false ? sightingsProjects[entity.Id] : null,
                SightingTypeId = entity.SightingTypeId,
                SightingTypeSearchGroupId = entity.SightingTypeSearchGroupId,
                PublicCollection = entity.OrganizationCollectorId.HasValue && _artportalenMetadataContainer.Organizations.ContainsKey(entity.OrganizationCollectorId.Value)
                    ? _artportalenMetadataContainer.Organizations[entity.OrganizationCollectorId.Value]
                    : null,
                PrivateCollection = entity.UserCollectorId.HasValue && _artportalenMetadataContainer.PersonByUserId.ContainsKey(entity.UserCollectorId.Value)
                    ? _artportalenMetadataContainer.PersonByUserId[entity.UserCollectorId.Value].FullName
                    : null,
                DeterminedBy = entity.DeterminerUserId.HasValue && _artportalenMetadataContainer.PersonByUserId.ContainsKey(entity.DeterminerUserId.Value) ? _artportalenMetadataContainer.PersonByUserId[entity.DeterminerUserId.Value].FullName : null,
                DeterminationYear = entity.DeterminationYear,
                ConfirmedBy = entity.ConfirmatorUserId.HasValue && _artportalenMetadataContainer.PersonByUserId.ContainsKey(entity.ConfirmatorUserId.Value) ? _artportalenMetadataContainer.PersonByUserId[entity.ConfirmatorUserId.Value].FullName : null,
                ConfirmationYear = entity.ConfirmationYear
            };

            observation.RegionalSightingStateId = entity.RegionalSightingStateId;
            observation.SightingPublishTypeIds = ConvertCsvStringToListOfIntegers(entity.SightingPublishTypeIds);
            observation.SpeciesFactsIds = ConvertCsvStringToListOfIntegers(entity.SpeciesFactsIds);

            if (personSightings.TryGetValue(entity.Id, out var personSighting))
            {
                observation.VerifiedBy = personSighting.VerifiedBy;
                observation.VerifiedByInternal = personSighting.VerifiedByInternal;
                observation.Observers = personSighting.Observers;
                observation.ObserversInternal = personSighting.ObserversInternal;
                observation.ReportedBy = personSighting.ReportedBy;
                observation.SpeciesCollection = personSighting.SpeciesCollection;
                observation.ReportedByUserId = personSighting.ReportedByUserId;
                observation.ReportedByUserAlias = personSighting.ReportedByUserAlias;
            }

            return observation;
        }

        private static List<int> ConvertCsvStringToListOfIntegers(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }

            var stringIds = s.Split(",");
            var ids = new List<int>();

            foreach (var stringId in stringIds)
            {
                if (int.TryParse(stringId, out var id))
                {
                    ids.Add(id);
                }
            }

            return ids.Any() ? ids : null;
        }

        #region Project
        
        /// <summary>
        ///     Cast project parameter itemEntity to aggregate
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private ProjectParameter CastProjectParameterEntityToVerbatim(ProjectParameterEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new ProjectParameter
            {
                Id = entity.ProjectParameterId,
                DataType = entity.DataType,
                Description = entity.Description,
                Name = entity.Name,
                Unit = entity.Unit,
                Value = entity.Value
            };
        }

        private async Task<IDictionary<int, Project[]>> GetSightingsProjects(IEnumerable<int> sightingIds, bool live = false)
        {
            if (!_artportalenMetadataContainer?.Projects?.Any() ?? true)
            {
                return null;
            }

            var sightingProjectIds = (await _sightingRepository.GetSightingProjectIdsAsync(sightingIds))?.ToArray();

            if (!sightingProjectIds?.Any() ?? true)
            {
                return null;
            }
            /*
            //if we are doing incremental harvesting then we need to make sure to fetch any new projects that may have been created
            if (live)
            {
                var projectEntities = (await _projectRepository.GetProjectsAsync(live))?.ToArray();
                _artportalenMetadataContainer.UpdateProjects(projectEntities);
            }*/
            // Cast a projects to verbatim
            var sightingsProjects = new Dictionary<int, IDictionary<int, Project>>();

            for (var i = 0; i < sightingProjectIds.Length; i++)
            {
                var (sightingId, projectId) = sightingProjectIds[i];

                if (!sightingsProjects.TryGetValue(sightingId, out var sightingProjects))
                {
                    sightingProjects = new Dictionary<int, Project>();
                    sightingsProjects.TryAdd(sightingId, sightingProjects);
                }

                if (!sightingProjects.ContainsKey(projectId))
                {
                    if (!_artportalenMetadataContainer.Projects.TryGetValue(projectId, out var project))
                    {
                        continue;
                    }

                    // Make a copy of project so we can add params to it later
                    sightingProjects.TryAdd(project.Id, project.Clone());
                }
            }

            var projectParameterEntities = (await _projectRepository.GetSightingProjectParametersAsync(sightingIds, IncrementalMode))?.ToArray();

            if (projectParameterEntities?.Any() ?? false)
            {
                for (var i = 0; i < projectParameterEntities.Length; i++)
                {
                    var projectParameterEntity = projectParameterEntities[i];
                    
                    // Try to get projects by sighting id
                    if (!sightingsProjects.TryGetValue(projectParameterEntity.SightingId, out var sightingProjects))
                    {
                        sightingProjects = new Dictionary<int, Project>();
                        sightingsProjects.TryAdd(projectParameterEntity.SightingId, sightingProjects);
                    }

                    // Try to get sighting project 
                    if (!sightingProjects.TryGetValue(projectParameterEntity.ProjectId, out var project))
                    {
                        if (!_artportalenMetadataContainer.Projects.TryGetValue(projectParameterEntity.ProjectId, out project))
                        {
                            continue;
                        }

                        project = project.Clone();
                        sightingProjects.TryAdd(project.Id, project);
                    }
                    project.ProjectParameters ??= new List<ProjectParameter>();
                    project.ProjectParameters.Add(CastProjectParameterEntityToVerbatim(projectParameterEntity));
                }
            }

            return sightingsProjects.Any() ? sightingsProjects.ToDictionary(sp => sp.Key, sp => sp.Value.Values.ToArray()) : null;
        }
        #endregion Project

        #region Site
        /// <summary>
        /// Try to add missing sites from live data
        /// </summary>
        /// <param name="siteIds"></param>
        /// <returns></returns>
        private async Task AddMissingSitesAsync(IEnumerable<int> siteIds)
        {
            if (!siteIds?.Any() ?? true)
            {
                return;
            }

            var siteEntities = await _siteRepository.GetByIdsAsync(siteIds, IncrementalMode);
            var siteAreas = await _siteRepository.GetSitesAreas(siteIds, IncrementalMode);
            var sitesGeometry = await _siteRepository.GetSitesGeometry(siteIds, IncrementalMode); // It's faster to get geometries in separate query than join it in site query

            var sites = CastSiteEntitiesToVerbatim(siteEntities?.ToArray(), siteAreas, sitesGeometry);

            if (sites?.Any() ?? false)
            {
                foreach (var site in sites)
                {
                    _sites.TryAdd(site.Id, site);
                }
            }
        }

        /// <summary>
        /// Cast multiple sites entities to models by continuously decreasing the siteEntities input list.
        ///     This saves about 500MB RAM when casting Artportalen sites (3 millions).
        /// </summary>
        /// <param name="siteEntities"></param>
        /// <param name="sitesAreas"></param>
        /// <param name="sitesGeometry"></param>
        /// <returns></returns>
        private IEnumerable<Site> CastSiteEntitiesToVerbatim(ICollection<SiteEntity> siteEntities, IDictionary<int, ICollection<AreaEntityBase>> sitesAreas, IDictionary<int, string> sitesGeometry)
        {
            var sites = new List<Site>();

            if (!siteEntities?.Any() ?? true)
            {
                return sites;
            }

            // Make sure metadata are initialized
            sitesAreas ??= new Dictionary<int, ICollection<AreaEntityBase>>();
            sitesGeometry ??= new Dictionary<int, string>();

            foreach (var siteEntity in siteEntities)
            {
                sitesAreas.TryGetValue(siteEntity.Id, out var siteAreas);
                sitesGeometry.TryGetValue(siteEntity.Id, out var geometryWkt);

                var site = CastSiteEntityToVerbatim(siteEntity, siteAreas, geometryWkt);

                if (site != null)
                {
                    sites.Add(site);
                }
            }
          
            return sites;
        }

        /// <summary>
        /// Cast site itemEntity to aggregate
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="areas"></param>
        /// <param name="geometryWkt"></param>
        /// <returns></returns>
        private Site CastSiteEntityToVerbatim(SiteEntity entity, ICollection<AreaEntityBase> areas, string geometryWkt)
        {
            if (entity == null)
            {
                return null;
            }

            Point wgs84Point = null;
            const int defaultAccuracy = 100;

            if (entity.XCoord > 0 && entity.YCoord > 0)
            {
                // We process point here since site is added to observation verbatim. One site can have multiple observations and by 
                // doing it here we only have to convert the point once
                var webMercatorPoint = new Point(entity.XCoord, entity.YCoord);
                wgs84Point = (Point)webMercatorPoint.Transform(CoordinateSys.WebMercator, CoordinateSys.WGS84);
            }

            Geometry siteGeometry = null;
            if (!string.IsNullOrEmpty(geometryWkt))
            {
                siteGeometry = geometryWkt.ToGeometry()
                    .Transform(CoordinateSys.WebMercator, CoordinateSys.WGS84).TryMakeValid();
            }

            var accuracy = entity.Accuracy > 0 ? entity.Accuracy : defaultAccuracy; // If Artportalen site accuracy is <= 0, this is due to an old import. Set the accuracy to 100.
            var site = new Site
            {
                Accuracy = accuracy,
                ExternalId = entity.ExternalId,
                Id = entity.Id,
                PresentationNameParishRegion = entity.PresentationNameParishRegion,
                Point = wgs84Point?.ToGeoJson(),
                PointWithBuffer = (siteGeometry?.IsValid() ?? false ? siteGeometry : wgs84Point?.ToCircle(accuracy))?.ToGeoJson(),
                Name = entity.Name,
                XCoord = entity.XCoord,
                YCoord = entity.YCoord,
                VerbatimCoordinateSystem = CoordinateSys.WebMercator,
                ParentSiteId = entity.ParentSiteId
            };

            if (!areas?.Any() ?? true)
            {
                return site;
            }

            foreach (var area in areas)
            {
                switch ((AreaType)area.AreaDatasetId)
                {
                    case AreaType.BirdValidationArea:
                        (site.BirdValidationAreaIds ??= new List<string>()).Add(area.FeatureId);
                        break;
                    case AreaType.County:
                        site.County = new GeographicalArea{FeatureId = area.FeatureId, Name = area.Name};
                        break;
                    case AreaType.Municipality:
                        site.Municipality = new GeographicalArea { FeatureId = area.FeatureId, Name = area.Name };
                        break;
                    case AreaType.Parish:
                        site.Parish = new GeographicalArea { FeatureId = area.FeatureId, Name = area.Name };
                        break;
                    case AreaType.Province:
                        site.Province = new GeographicalArea { FeatureId = area.FeatureId, Name = area.Name };
                        break;
                }
            }

            _areaHelper.AddAreaDataToSite(site);

            return site;
        }
        #endregion Site

        #region SightingRelations
        /// <summary>
        /// Cast sighting relations to verbatim
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        private static IEnumerable<SightingRelation> CastSightingRelationsToVerbatim(IEnumerable<SightingRelationEntity> entities)
        {
            if (!entities?.Any() ?? true)
            {
                return null;
            }

            return from e in entities
                   select new SightingRelation
                   {
                       DeterminationYear = e.DeterminationYear,
                       EditDate = e.EditDate,
                       Id = e.Id,
                       IsPublic = e.IsPublic,
                       RegisterDate = e.RegisterDate,
                       SightingId = e.SightingId,
                       SightingRelationTypeId = e.SightingRelationTypeId,
                       Sort = e.Sort,
                       UserId = e.UserId
                   }; 
        }
        #endregion SightingRelations

        #region SpeciesCollections
        private IEnumerable<SpeciesCollectionItem> CastSpeciesCollectionsToVerbatim(
            IEnumerable<SpeciesCollectionItemEntity> entities)
        {
            if (!entities?.Any() ?? true)
            {
                return null;
            }

            return from s in entities
                select new SpeciesCollectionItem
                {
                    SightingId = s.SightingId,
                    CollectorId = s.CollectorId,
                    OrganizationId = s.OrganizationId,
                    DeterminerText = s.DeterminerText,
                    DeterminerYear = s.DeterminerYear,
                    Description = s.Description,
                    ConfirmatorText = s.ConfirmatorText,
                    ConfirmatorYear = s.ConfirmatorYear
                };
        }

        /// <summary>
        /// Initialize species collections
        /// </summary>
        /// <param name="sightingIds"></param>
        /// <returns></returns>
        private async Task<IList<SpeciesCollectionItem>> GetSpeciesCollections(IEnumerable<int> sightingIds)
        {
            return CastSpeciesCollectionsToVerbatim(await _speciesCollectionRepository.GetBySightingAsync(sightingIds, IncrementalMode))?.ToList();
        }
        #endregion SpeciesCollections


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="projectRepository"></param>
        /// <param name="sightingRepository"></param>
        /// <param name="siteRepository"></param>
        /// <param name="sightingRelationRepository"></param>
        /// <param name="speciesCollectionRepository"></param>
        /// <param name="artportalenMetadataContainer"></param>
        /// <param name="areaHelper"></param>
        public ArtportalenHarvestFactory(
            IProjectRepository projectRepository,
            ISightingRepository sightingRepository,
            ISiteRepository siteRepository,
            ISightingRelationRepository sightingRelationRepository,
            ISpeciesCollectionItemRepository speciesCollectionRepository,
            IArtportalenMetadataContainer artportalenMetadataContainer,
            IAreaHelper areaHelper) : base()
        {
            _projectRepository = projectRepository;
            _sightingRepository = sightingRepository;
            _siteRepository = siteRepository;
            _sightingRelationRepository = sightingRelationRepository;
            _speciesCollectionRepository = speciesCollectionRepository;
            _artportalenMetadataContainer = artportalenMetadataContainer;
            _areaHelper = areaHelper;
            _sites = new ConcurrentDictionary<int, Site>();
        }

        public bool IncrementalMode { get; set; }
        
        /// <inheritdoc />
        public async Task<IEnumerable<ArtportalenObservationVerbatim>> CastEntitiesToVerbatimsAsync(SightingEntity[] entities)
        {
            if (!entities?.Any() ?? true)
            {
                return null;
            }

            var sightingIds = new HashSet<int>();
            var newSiteIds = new HashSet<int>();

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                sightingIds.Add(entity.Id);
                var siteId = entity.SiteId ?? 0;

                // Check for new sites since we already lopping the array 
                if (siteId == 0 || newSiteIds.Contains(siteId) || _sites.ContainsKey(siteId))
                {
                    continue;
                }

                newSiteIds.Add(siteId);
            }
            
            await AddMissingSitesAsync(newSiteIds);
            var sightingsProjects = await GetSightingsProjects(sightingIds, IncrementalMode);

            // Get Observers, ReportedBy, SpeciesCollection & VerifiedBy
            var sightingRelations =
                CastSightingRelationsToVerbatim(await _sightingRelationRepository.GetAsync(sightingIds, IncrementalMode))?.ToArray();

            var speciesCollections = await GetSpeciesCollections(sightingIds);

            var personSightings = PersonSightingFactory.CreatePersonSightingDictionary(
                sightingIds,
                _artportalenMetadataContainer.PersonByUserId,
                _artportalenMetadataContainer.OrganizationById,
                speciesCollections,
                sightingRelations);

            var verbatims = new List<ArtportalenObservationVerbatim>();
            for (var i = 0; i < entities.Length; i++)
            {
                verbatims.Add(CastEntityToVerbatim(entities[i], personSightings, sightingsProjects));
            }

            return verbatims;
        }

    }
}
