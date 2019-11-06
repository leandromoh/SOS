﻿using System.Linq;
using System.Security.Authentication;
using Autofac;
using MongoDB.Driver;
using SOS.Lib.Configuration.Import;
using SOS.Lib.Configuration.Shared;
using SOS.Import.Factories;
using SOS.Import.Factories.Interfaces;
using SOS.Import.Jobs;
using SOS.Import.Jobs.Interfaces;
using SOS.Import.MongoDb;
using SOS.Import.MongoDb.Interfaces;
using SOS.Import.Repositories.Destination.ClamTreePortal;
using SOS.Import.Repositories.Destination.ClamTreePortal.Interfaces;
using SOS.Import.Repositories.Destination.Kul;
using SOS.Import.Repositories.Destination.Kul.Interfaces;
using SOS.Import.Repositories.Destination.SpeciesPortal;
using SOS.Import.Repositories.Destination.SpeciesPortal.Interfaces;
using SOS.Import.Repositories.Source.Kul;
using SOS.Import.Repositories.Source.Kul.Interfaces;
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
            builder.RegisterInstance(Configuration.ClamTreeServiceConfiguration).As<ClamTreeServiceConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.KulServiceConfiguration).As<KulServiceConfiguration>().SingleInstance();
            builder.RegisterInstance(Configuration.SpeciesPortalConfiguration).As<SpeciesPortalConfiguration>().SingleInstance();

            // Init mongodb
            var importSettings = GetMongDbSettings(Configuration.MongoDbConfiguration);
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
            builder.RegisterType<KulObservationRepository>().As<IKulObservationRepository>().InstancePerLifetimeScope();

            // Repositories destination
            builder.RegisterType<AreaVerbatimRepository>().As<IAreaVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<SightingVerbatimRepository>().As<ISightingVerbatimRepository>().InstancePerLifetimeScope();

            builder.RegisterType<ClamObservationVerbatimRepository>().As<IClamObservationVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<TreeObservationVerbatimRepository>().As<ITreeObservationVerbatimRepository>().InstancePerLifetimeScope();
            builder.RegisterType<KulObservationVerbatimRepository>().As<IKulObservationVerbatimRepository>().InstancePerLifetimeScope();

            // Add factories
            builder.RegisterType<ClamTreePortalObservationFactory>().As<IClamTreePortalObservationFactory>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalSightingFactory>().As<ISpeciesPortalSightingFactory>().InstancePerLifetimeScope();
            builder.RegisterType<KulObservationFactory>().As<IKulObservationFactory>().InstancePerLifetimeScope();

            // Add Services
            builder.RegisterType<ClamTreeObservationService>().As<IClamTreeObservationService>().InstancePerLifetimeScope();
            builder.RegisterType<HttpClientService>().As<IHttpClientService>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalDataService>().As<ISpeciesPortalDataService>().InstancePerLifetimeScope();

            // Add jobs
            builder.RegisterType<ClamTreePortalHarvestJob>().As<IClamTreePortalHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<SpeciesPortalHarvestJob>().As<ISpeciesPortalHarvestJob>().InstancePerLifetimeScope();
            builder.RegisterType<KulHarvestJob>().As<IKulHarvestJob>().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Get mongo db settings object
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static MongoClientSettings GetMongDbSettings(MongoDbConfiguration config)
        {
            MongoInternalIdentity identity = null;
            PasswordEvidence evidence = null;
            if (!(string.IsNullOrEmpty(config.DatabaseName) ||
                  string.IsNullOrEmpty(config.UserName) ||
                  string.IsNullOrEmpty(config.Password)))
            {
                identity = new MongoInternalIdentity(config.DatabaseName, config.UserName);
                evidence = new PasswordEvidence(config.Password);
            }

            return new MongoClientSettings
            {
                Servers = config.Hosts.Select(h => new MongoServerAddress(h.Name, h.Port)),
                ReplicaSetName = config.ReplicaSetName,
                UseTls = config.UseTls,
                SslSettings = config.UseTls ? new SslSettings
                {
                    EnabledSslProtocols = SslProtocols.Tls12
                } : null,
                Credential = identity != null && evidence != null ? new MongoCredential("SCRAM-SHA-1", identity, evidence) : null
            };
        }
    }
}
