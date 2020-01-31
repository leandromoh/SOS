﻿using Autofac;
using SOS.Lib.Configuration.Import;
using SOS.Import.Factories;
using SOS.Import.Factories.Interfaces;
using SOS.Import.Jobs;
using SOS.Import.Jobs.Interfaces;
using SOS.Import.MongoDb;
using SOS.Import.MongoDb.Interfaces;
using SOS.Import.Repositories.Destination;
using SOS.Import.Repositories.Destination.ClamPortal;
using SOS.Import.Repositories.Destination.ClamPortal.Interfaces;
using SOS.Import.Repositories.Destination.FieldMappings;
using SOS.Import.Repositories.Destination.FieldMappings.Interfaces;
using SOS.Import.Repositories.Destination.Interfaces;
using SOS.Import.Repositories.Destination.Kul;
using SOS.Import.Repositories.Destination.Kul.Interfaces;
using SOS.Import.Repositories.Destination.SpeciesPortal;
using SOS.Import.Repositories.Destination.SpeciesPortal.Interfaces;
using SOS.Import.Repositories.Destination.Taxon;
using SOS.Import.Repositories.Destination.Taxon.Interfaces;
using SOS.Import.Repositories.Source.SpeciesPortal;
using SOS.Import.Repositories.Source.SpeciesPortal.Interfaces;
using SOS.Import.Services;
using SOS.Import.Services.Interfaces;

namespace SOS.Import.IoC.Modules
{
    public class ImportModule : Module
    {
        public ImportConfiguration Configuration { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            // Add configuration
            builder.RegisterInstance(Configuration.ClamServiceConfiguration).As<ClamServiceConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.KulServiceConfiguration).As<KulServiceConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.SpeciesPortalConfiguration).As<SpeciesPortalConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.TaxonAttributeServiceConfiguration).As<TaxonAttributeServiceConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.TaxonServiceConfiguration).As<TaxonServiceConfiguration>().SingleInstance();

            // Init mongodb
            var importSettings = Configuration.MongoDbConfiguration.GetMongoDbSettings();
            var importClient = new ImportClient(importSettings, Configuration.MongoDbConfiguration.DatabaseName, Configuration.MongoDbConfiguration.BatchSize);
            builder.RegisterInstance(importClient).As<IImportClient>().SingleInstance();
            
            // Repositories source
            builder.RegisterType<AreaRepository>().As<IAreaRepository>().InstancePerLifetimeScope();
            builder.RegisterType<MetadataRepository>().As<IMetadataRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ProjectRepository>().As<IProjectRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SightingRepository>().As<ISightingRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SiteRepository>().As<ISiteRepository>().InstancePerLifetimeScope();
            builder.RegisterType<OrganizationRepository>().As<IOrganizationRepository>().InstancePerLifetimeScope();
            builder.RegisterType<PersonRepository>().As<IPersonRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SightingRelationRepository>().As<ISightingRelationRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesCollectionItemRepository>().As<ISpeciesCollectionItemRepository>().InstancePerLifetimeScope();
            builder.RegisterType<KulObservationService>().As<IKulObservationService>().InstancePerLifetimeScope();

            // Repositories destination
            builder.RegisterType<AreaVerbatimRepository>().As<IAreaVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ClamObservationVerbatimRepository>().As<IClamObservationVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<HarvestInfoRepository>().As<IHarvestInfoRepository>().InstancePerLifetimeScope();
            builder.RegisterType<KulObservationVerbatimRepository>().As<IKulObservationVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SightingVerbatimRepository>().As<ISightingVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonVerbatimRepository>().As<ITaxonVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<FieldMappingRepository>().As<IFieldMappingRepository>().InstancePerLifetimeScope();

            // Add factories
            builder.RegisterType<ClamPortalObservationFactory>().As<IClamPortalObservationFactory>().InstancePerLifetimeScope();
            builder.RegisterType<GeoFactory>().As<IGeoFactory>().InstancePerLifetimeScope();
            builder.RegisterType<KulObservationFactory>().As<IKulObservationFactory>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalSightingFactory>().As<ISpeciesPortalSightingFactory>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonFactory>().As<ITaxonFactory>().InstancePerLifetimeScope();
            builder.RegisterType<FieldMappingFactory>().As<IFieldMappingFactory>().InstancePerLifetimeScope();
            

            // Add Services
            builder.RegisterType<ClamObservationService>().As<IClamObservationService>().InstancePerLifetimeScope();
            builder.RegisterType<HttpClientService>().As<IHttpClientService>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalDataService>().As<ISpeciesPortalDataService>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonServiceProxy>().As<ITaxonServiceProxy>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonService>().As<ITaxonService>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonAttributeService>().As<ITaxonAttributeService>().InstancePerLifetimeScope();

            // Add jobs
            builder.RegisterType<ClamPortalHarvestJob>().As<IClamPortalHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<GeoHarvestJob>().As<IGeoHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<KulHarvestJob>().As<IKulHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalHarvestJob>().As<ISpeciesPortalHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonHarvestJob>().As<ITaxonHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<FieldMappingImportJob>().As<IFieldMappingImportJob>().InstancePerLifetimeScope();
        }
    }
}
