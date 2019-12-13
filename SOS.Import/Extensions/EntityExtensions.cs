﻿using System;
using System.Collections.Generic;
using System.Linq;
using SOS.Import.Entities;
using SOS.Lib.Enums;
using SOS.Lib.Extensions;
using SOS.Lib.Models.Verbatim.SpeciesPortal;
using SOS.Lib.Models.Verbatim.Shared;

namespace SOS.Import.Extensions
{
    /// <summary>
    /// Entity extensions
    /// </summary>
    public static class SightingExtensions
    {
        /// <summary>
        /// Cast area entity to model 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Area ToAggregate(this AreaEntity entity)
        {
            return new Area((AreaType)entity.AreaDatasetId)
            {
                Id = entity.Id,
                FeatureId = entity.FeatureId,
                ParentId = entity.ParentId,
                Geometry = entity.Polygon.ToGeometry().Transform(CoordinateSys.WebMercator, CoordinateSys.WGS84).ToGeoJsonGeometry(),
                Name = entity.Name
            };
        }

        /// <summary>
        /// Cast multiple area entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<Area> ToAggregates(this IEnumerable<AreaEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast multiple sightings entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="activities"></param>
        /// <param name="biotopes"></param>
        /// <param name="genders"></param>
        /// <param name="organizations"></param>
        /// <param name="personSightings"></param>
        /// <param name="sites"></param>
        /// <param name="stages"></param>
        /// <param name="substrates"></param>
        /// <param name="validationStatus"></param>
        /// <param name="units"></param>
        /// <param name="projectEntityDictionaries"></param>
        /// <returns></returns>
        public static IEnumerable<APSightingVerbatim> ToAggregates(this IEnumerable<SightingEntity> entities,
            IDictionary<int, MetadataWithCategory> activities,
            IDictionary<int, Metadata> biotopes,
            IDictionary<int, Metadata> genders,
            IDictionary<int, Metadata> organizations,
            IDictionary<int, PersonSighting> personSightings,
            IDictionary<int, Site> sites,
            IDictionary<int, Metadata> stages,
            IDictionary<int, Metadata> substrates,
            IDictionary<int, Metadata> validationStatus,
            IDictionary<int, Metadata> units,
            ProjectEntityDictionaries projectEntityDictionaries)
        {
            return entities.Select(e => e.ToAggregate(
                activities,
                biotopes,
                genders,
                organizations,
                personSightings,
                sites,
                stages,
                substrates,
                validationStatus,
                units,
                projectEntityDictionaries));
        }

        /// <summary>
        /// Cast sighting itemEntity to model 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="activities"></param>
        /// <param name="biotopes"></param>
        /// <param name="genders"></param>
        /// <param name="organizations"></param>
        /// <param name="personSightings"></param>
        /// <param name="sites"></param>
        /// <param name="stages"></param>
        /// <param name="substrates"></param>
        /// <param name="validationStatus"></param>
        /// <param name="units"></param>
        /// <param name="projectEntityDictionaries"></param>
        /// <returns></returns>
        public static APSightingVerbatim ToAggregate(this SightingEntity entity,
            IDictionary<int, MetadataWithCategory> activities,
            IDictionary<int, Metadata> biotopes,
            IDictionary<int, Metadata> genders,
            IDictionary<int, Metadata> organizations,
            IDictionary<int, PersonSighting> personSightings,
            IDictionary<int, Site> sites,
            IDictionary<int, Metadata> stages,
            IDictionary<int, Metadata> substrates,
            IDictionary<int, Metadata> validationStatus,
            IDictionary<int, Metadata> units, 
            ProjectEntityDictionaries projectEntityDictionaries)
        {
            var observation = new APSightingVerbatim
            {
                Activity = entity.ActivityId.HasValue && activities.ContainsKey(entity.ActivityId.Value)
                    ? activities[entity.ActivityId.Value]
                    : null,
                Bioptope = entity.BiptopeId.HasValue && biotopes.ContainsKey(entity.BiptopeId.Value)
                    ? biotopes[entity.BiptopeId.Value]
                    : null,
                BiotopeDescription = entity.BiptopeDescription,
                CollectionID = entity.CollectionID,
                Comment = entity.Comment,
                ControlingOrganisationId = entity.ControlingOrganisationId,
                EndDate = entity.EndDate,
                EndTime = entity.EndTime,
                Gender = entity.GenderId.HasValue && genders.ContainsKey(entity.GenderId.Value)
                    ? genders[entity.GenderId.Value]
                    : null,
                HasImages = entity.HasImages,
                HiddenByProvider = entity.HiddenByProvider,
                Id = entity.Id,
                OwnerOrganization =
                    entity.OwnerOrganizationId.HasValue &&
                    organizations.ContainsKey(entity.OwnerOrganizationId.Value)
                        ? organizations[entity.OwnerOrganizationId.Value]
                        : null,
                Label = entity.Label,
                Length = entity.Length,
                MaxDepth = entity.MaxDepth,
                MaxHeight = entity.MaxHeight,
                MigrateSightingObsId = entity.MigrateSightingObsId,
                MigrateSightingPortalId = entity.MigrateSightingPortalId,
                MinDepth = entity.MinDepth,
                MinHeight = entity.MinHeight,
                NotPresent = entity.NotPresent,
                NotRecovered = entity.NotRecovered,
                ProtectedBySystem = entity.ProtectedBySystem,
                Quantity = entity.Quantity,
                QuantityOfSubstrate = entity.QuantityOfSubstrate,
                ReportedDate = entity.RegisterDate,
                RightsHolder = entity.RightsHolder,
                Site = entity.SiteId.HasValue && sites.ContainsKey(entity.SiteId.Value)
                    ? sites[entity.SiteId.Value]
                    : null,
                Stage = entity.StageId.HasValue && stages.ContainsKey(entity.StageId.Value)
                    ? stages[entity.StageId.Value]
                    : null,
                StartDate = entity.StartDate,
                StartTime = entity.StartTime,
                Substrate = entity.SubstrateId.HasValue && substrates.ContainsKey(entity.SubstrateId.Value)
                    ? substrates[entity.SubstrateId.Value]
                    : null,
                SubstrateDescription = entity.SubstrateDescription,
                SubstrateSpeciesDescription = entity.SubstrateSpeciesDescription,
                SubstrateSpeciesId = entity.SubstrateSpeciesId,
                TaxonId = entity.TaxonId,
                Unit = entity.UnitId.HasValue && units.ContainsKey(entity.UnitId.Value)
                    ? units[entity.UnitId.Value]
                    : null,
                Unspontaneous = entity.Unspontaneous,
                UnsureDetermination = entity.UnsureDetermination,
                URL = entity.URL,
                ValidationStatus = validationStatus.ContainsKey(entity.ValidationStatusId)
                    ? validationStatus[entity.ValidationStatusId]
                    : null,
                Weight = entity.Weight,
                Projects = GetProjects(
                    entity.Id,
                    projectEntityDictionaries)
            };

            if (personSightings.TryGetValue(entity.Id, out PersonSighting personSighting))
            {
                observation.VerifiedBy = personSighting.VerifiedBy;
                observation.Observers = personSighting.Observers;
                observation.ReportedBy = personSighting.ReportedBy;
                observation.SpeciesCollection = personSighting.SpeciesCollection;
            }

            return observation;
        }

        /// <summary>
        /// Get project and project parameters for the specified sightingId.
        /// </summary>
        private static List<Project> GetProjects(int sightingId, ProjectEntityDictionaries projectEntityDictionaries)
        {
            Dictionary<int, Project> projectById = null;
            if (projectEntityDictionaries.ProjectEntitiesBySightingId.TryGetValue(sightingId, out var projectEntities))
            {
                projectById = projectEntities.ToAggregates().ToDictionary(p => p.Id, p => p);
            }

            if (projectEntityDictionaries.ProjectParameterEntitiesBySightingId.TryGetValue(sightingId, out var projectParameterEntities))
            {
                if (projectById == null)
                {
                    projectById = new Dictionary<int, Project>();
                }

                foreach (var projectParameterEntity in projectParameterEntities)
                {
                    if (projectById.TryGetValue(projectParameterEntity.ProjectId, out var project))
                    {
                        if (project.ProjectParameters == null)
                        {
                            project.ProjectParameters = new List<ProjectParameter>();
                        }

                        project.ProjectParameters.Add(projectParameterEntity.ToAggregate());
                    }
                    else
                    {
                        if (projectEntityDictionaries.ProjectEntityById.TryGetValue(projectParameterEntity.ProjectId, out var projectEntity))
                        {
                            var newProject = projectEntity.ToAggregate();
                            newProject.ProjectParameters = new List<ProjectParameter>();
                            newProject.ProjectParameters.Add(projectParameterEntity.ToAggregate());
                            projectById.Add(projectParameterEntity.ProjectId, newProject);
                        }
                    }
                }
            }

            if (projectById == null || projectById.Keys.Count == 0)
            {
                return null;
            }

            return projectById.Values.ToList();
        }

        /// <summary>
        /// Cast meta data itemEntity to model 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Metadata ToAggregate(this MetadataEntity entity)
        {
            return new Metadata
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        /// <summary>
        /// Cast multiple sightings entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<Metadata> ToAggregates(this IEnumerable<MetadataEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast meta data itemEntity to model 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static MetadataWithCategory ToAggregate(this MetadataWithCategoryEntity entity)
        {
            return new MetadataWithCategory()
            {
                Id = entity.Id,
                Name = entity.Name,
                Category = new Metadata
                {
                    Id = entity.CategoryId,
                    Name = entity.CategoryName
                }
            };
        }

        /// <summary>
        /// Cast multiple sightings entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<MetadataWithCategory> ToAggregates(this IEnumerable<MetadataWithCategoryEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast project entity to aggregate
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Project ToAggregate(this ProjectEntity entity)
        {
            return new Project()
            {
                Category = entity.Category,
                Description = entity.Description,
                EndDate = entity.EndDate,
                Id = entity.Id,
                IsPublic = entity.IsPublic,
                Name = entity.Name,
                Owner = entity.Owner,
                StartDate = entity.StartDate,
                SurveyMethod = entity.SurveyMethod,
                SurveyMethodUrl = entity.SurveyMethodUrl
            };
        }

        /// <summary>
        /// Cast multiple projects entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<Project> ToAggregates(this IEnumerable<ProjectEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast multiple projects to aggregates
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<Organization> ToAggregates(this IEnumerable<OrganizationEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        public static Organization ToAggregate(this OrganizationEntity entity)
        {
            return new Organization
            {
                Id = entity.Id,
                Name = entity.Name,
                OrganizationId = entity.OrganizationId
            };
        }

        public static IEnumerable<SightingRelation> ToAggregates(this IEnumerable<SightingRelationEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        public static SightingRelation ToAggregate(this SightingRelationEntity entity)
        {
            return new SightingRelation
            {
                DeterminationYear = entity.DeterminationYear,
                EditDate = entity.EditDate,
                Id = entity.Id,
                IsPublic = entity.IsPublic,
                RegisterDate = entity.RegisterDate,
                SightingId = entity.SightingId,
                SightingRelationTypeId = entity.SightingRelationTypeId,
                Sort = entity.Sort,
                UserId = entity.UserId
            };
        }

        public static IEnumerable<Person> ToAggregates(this IEnumerable<PersonEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        public static Person ToAggregate(this PersonEntity entity)
        {
            return new Person()
            {
                Id = entity.Id,
                UserId = entity.UserId,
                FirstName = entity.FirstName,
                LastName = entity.LastName
            };
        }

        public static IEnumerable<SpeciesCollectionItem> ToAggregates(this IEnumerable<SpeciesCollectionItemEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        public static SpeciesCollectionItem ToAggregate(this SpeciesCollectionItemEntity itemEntity)
        {
            return new SpeciesCollectionItem()
            {
                SightingId = itemEntity.SightingId,
                CollectorId = itemEntity.CollectorId,
                OrganizationId = itemEntity.OrganizationId,
                DeterminerText = itemEntity.DeterminerText,
                DeterminerYear = itemEntity.DeterminerYear,
                Description = itemEntity.Description,
                ConfirmatorText = itemEntity.ConfirmatorText,
                ConfirmatorYear = itemEntity.ConfirmatorYear
            };
        }

        /// <summary>
        /// Cast site itemEntity to aggregate
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Site ToAggregate(this SiteEntity entity)
        {
            return new Site
            {
                Accuracy = entity.Accuracy,
                County = entity.CountyId.HasValue ? new Metadata{ Id = entity.CountyId.Value, Name = entity.CountyName } : null,
                CountryPart = entity.CountryPartId.HasValue ? new Metadata { Id = entity.CountryPartId.Value, Name = entity.CountryPartName } : null,
                Id = entity.Id,
                Municipality = entity.MunicipalityId.HasValue ? new Metadata { Id = entity.MunicipalityId.Value, Name = entity.MunicipalityName } : null,
                Province = entity.ProvinceId.HasValue ? new Metadata { Id = entity.ProvinceId.Value, Name = entity.ProvinceName } : null,
                Parish = entity.ParishId.HasValue ? new Metadata { Id = entity.ParishId.Value, Name = entity.ParishName } : null,
                Name = entity.Name,
                XCoord = entity.XCoord,
                YCoord = entity.YCoord
            };
        }

        /// <summary>
        /// Cast multiple sites entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<Site> ToAggregates(this IEnumerable<SiteEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast multiple project parameter entities to models 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static IEnumerable<ProjectParameter> ToAggregates(this IEnumerable<ProjectParameterEntity> entities)
        {
            return entities.Select(e => e.ToAggregate());
        }

        /// <summary>
        /// Cast project parameter itemEntity to aggregate
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ProjectParameter ToAggregate(this ProjectParameterEntity entity)
        {
            return new ProjectParameter
            {
                SightingId = entity.SightingId,
                ProjectId = entity.ProjectId,
                ProjectParameterId = entity.ProjectParameterId,
                DataType = entity.DataType,
                Description = entity.Description,
                Name = entity.Name, 
                Unit = entity.Unit, 
                Value = entity.Value
            };
        }
    }
}
