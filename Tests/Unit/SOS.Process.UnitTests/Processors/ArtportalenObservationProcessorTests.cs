using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Lib.Configuration.Process;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Artportalen;
using SOS.Process.Helpers.Interfaces;
using SOS.Process.Processors.Artportalen;
using SOS.Process.Repositories.Destination.Interfaces;
using SOS.Process.Repositories.Source.Interfaces;
using Xunit;

namespace SOS.Process.UnitTests.Processors
{
    /// <summary>
    /// Tests for Artportalen processor
    /// </summary>
    public class ArtportalenObservationProcessorTests
    {
        private readonly Mock<IArtportalenVerbatimRepository> _artportalenVerbatimRepository;
        private readonly Mock<IProcessedObservationRepository> _processedObservationRepositoryMock;
        private readonly Mock<IProcessedFieldMappingRepository> _processedFieldMappingRepositoryMock;
        private readonly Mock<IFieldMappingResolverHelper> _fieldMappingResolverHelperMock;
        private readonly ProcessConfiguration _processConfiguration;
        private readonly Mock<ILogger<ArtportalenObservationProcessor>> _loggerMock;

        /// <summary>
        /// Constructor
        /// </summary>
        public ArtportalenObservationProcessorTests()
        {
            _artportalenVerbatimRepository = new Mock<IArtportalenVerbatimRepository>();
            _processedObservationRepositoryMock = new Mock<IProcessedObservationRepository>();
            _processedFieldMappingRepositoryMock = new Mock<IProcessedFieldMappingRepository>();
            _fieldMappingResolverHelperMock = new Mock<IFieldMappingResolverHelper>();
            _processConfiguration = new ProcessConfiguration();
            _loggerMock = new Mock<ILogger<ArtportalenObservationProcessor>>();
        }

        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object).Should().NotBeNull();

            Action create = () => new ArtportalenObservationProcessor(
                null,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("artportalenVerbatimRepository");

            create = () => new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                null,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("DarwinCoreRepository");

            create = () => new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("processConfiguration");

            create = () => new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        /// <summary>
        /// Make a successful test of processing
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessAsyncSuccess()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------

            _artportalenVerbatimRepository.Setup(r => r.GetBatchAsync(0, 0))
                .ReturnsAsync(new [] { new ArtportalenVerbatimObservation
                {
                    Id = 1
                } });

            _processedObservationRepositoryMock.Setup(r => r.AddManyAsync(It.IsAny<ICollection<ProcessedObservation>>()))
                .ReturnsAsync(1);

            var taxa = new Dictionary<int, ProcessedTaxon>
            {
                { 0, new ProcessedTaxon { Id = 0, TaxonId = "0", ScientificName = "Biota" } }
            };

            var fieldMappingById = new Dictionary<int, FieldMapping>
            {
                {0, new FieldMapping {Id = 0, Name = "ActivityId"}}
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var artportalenProcessor = new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object);

            var result = await artportalenProcessor.ProcessAsync(taxa, JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Success);
        }

        /// <summary>
        /// Test processing fail
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AggregateAsyncFail()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
           
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var artportalenProcessor = new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object);

            var result = await artportalenProcessor.ProcessAsync(null, JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Failed);
        }

        /// <summary>
        /// Test processing exception
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ProcessAsyncException()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------

            _artportalenVerbatimRepository.Setup(r => r.GetBatchAsync(0, 0))
                .ThrowsAsync(new Exception("Failed"));
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var artportalenProcessor = new ArtportalenObservationProcessor(
                _artportalenVerbatimRepository.Object,
                _processedObservationRepositoryMock.Object,
                _processedFieldMappingRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _processConfiguration,
                _loggerMock.Object);

            var result = await artportalenProcessor.ProcessAsync(null, JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Failed);
        }
    }
}