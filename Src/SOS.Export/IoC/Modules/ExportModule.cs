﻿using Autofac;
using SOS.Export.IO.DwcArchive;
using SOS.Export.IO.DwcArchive.Interfaces;
using SOS.Export.Jobs;
using SOS.Export.Managers;
using SOS.Export.Managers.Interfaces;
using SOS.Export.MongoDb;
using SOS.Export.MongoDb.Interfaces;
using SOS.Export.Repositories;
using SOS.Export.Repositories.Interfaces;
using SOS.Export.Services;
using SOS.Export.Services.Interfaces;
using SOS.Lib.Configuration.Export;
using SOS.Lib.Configuration.Shared;
using SOS.Lib.Jobs.Export;

namespace SOS.Export.IoC.Modules
{
    /// <summary>
    ///     Export module
    /// </summary>
    public class ExportModule : Module
    {
        /// <summary>
        ///     Module configuration
        /// </summary>
        public ExportConfiguration Configuration { get; set; }

        /// <summary>
        ///     Load event
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {
            // Add configuration
            builder.RegisterInstance(Configuration.BlobStorageConfiguration).As<BlobStorageConfiguration>()
                .SingleInstance();
            builder.RegisterInstance(Configuration.FileDestination).As<FileDestination>().SingleInstance();
            builder.RegisterInstance(Configuration.ZendToConfiguration).As<ZendToConfiguration>().SingleInstance();

            // Init mongodb
            var exportSettings = Configuration.ProcessedDbConfiguration.GetMongoDbSettings();
            var exportClient = new ExportClient(exportSettings, Configuration.ProcessedDbConfiguration.DatabaseName,
                Configuration.ProcessedDbConfiguration.BatchSize);
            builder.RegisterInstance(exportClient).As<IExportClient>().SingleInstance();

            // Add factories
            builder.RegisterType<ObservationManager>().As<IObservationManager>().InstancePerLifetimeScope();
            builder.RegisterType<TaxonManager>().As<ITaxonManager>().InstancePerLifetimeScope();

            // Repositories mongo
            builder.RegisterType<DOIRepository>().As<IDOIRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ProcessedObservationRepository>().As<IProcessedObservationRepository>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ProcessedTaxonRepository>().As<IProcessedTaxonRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ProcessInfoRepository>().As<IProcessInfoRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ProcessedFieldMappingRepository>().As<IProcessedFieldMappingRepository>()
                .InstancePerLifetimeScope();

            // Services
            builder.RegisterType<BlobStorageService>().As<IBlobStorageService>().InstancePerLifetimeScope();
            builder.RegisterType<FileService>().As<IFileService>().InstancePerLifetimeScope();
            builder.RegisterType<ZendToService>().As<IZendToService>().InstancePerLifetimeScope();

            // Add jobs
            builder.RegisterType<ExportAndSendJob>().As<IExportAndSendJob>().InstancePerLifetimeScope();
            builder.RegisterType<ExportAndStoreJob>().As<IExportAndStoreJob>().InstancePerLifetimeScope();

            // DwC Archive
            builder.RegisterType<DwcArchiveFileWriterCoordinator>().As<IDwcArchiveFileWriterCoordinator>().SingleInstance();
            builder.RegisterType<DwcArchiveFileWriter>().As<IDwcArchiveFileWriter>().SingleInstance();
            builder.RegisterType<DwcArchiveOccurrenceCsvWriter>().As<IDwcArchiveOccurrenceCsvWriter>().SingleInstance();
            builder.RegisterType<ExtendedMeasurementOrFactCsvWriter>().As<IExtendedMeasurementOrFactCsvWriter>()
                .SingleInstance();
        }
    }
}