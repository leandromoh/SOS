using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Verbatim.ClamPortal;
using SOS.Process.Helpers.Interfaces;
using SOS.Process.Processors.ClamPortal;
using SOS.Process.Repositories.Destination.Interfaces;
using SOS.Process.Repositories.Source.Interfaces;
using Xunit;

namespace SOS.Process.UnitTests.Processors
{
    /// <summary>
    /// Tests for Clam Portal processor
    /// </summary>
    public class ClamPortalObservationProcessorTests
    {
        private readonly Mock<IClamObservationVerbatimRepository> _clamObservationVerbatimRepositoryMock;
        private readonly Mock<IAreaHelper> _areaHelper;
        private readonly Mock<IProcessedObservationRepository> _processedObservationRepositoryMock;
        private readonly Mock<IFieldMappingResolverHelper> _fieldMappingResolverHelperMock;
        private readonly Mock<ILogger<ClamPortalObservationProcessor>> _loggerMock;

        /// <summary>
        /// Constructor
        /// </summary>
        public ClamPortalObservationProcessorTests()
        {
            _clamObservationVerbatimRepositoryMock = new Mock<IClamObservationVerbatimRepository>();
            _areaHelper = new Mock<IAreaHelper>();
            _processedObservationRepositoryMock = new Mock<IProcessedObservationRepository>();
            _fieldMappingResolverHelperMock = new Mock<IFieldMappingResolverHelper>();
            _loggerMock = new Mock<ILogger<ClamPortalObservationProcessor>>();
        }

        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object).Should().NotBeNull();

            Action create = () => new ClamPortalObservationProcessor(
                null,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("clamObservationVerbatimRepository");


            create = () => new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                null,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("areaHelper");

            create = () => new ClamPortalObservationProcessor(
                 _clamObservationVerbatimRepositoryMock.Object,
                 _areaHelper.Object,
                null,
                 _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("DarwinCoreRepository");

            create = () => new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
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
            _clamObservationVerbatimRepositoryMock.Setup(r => r.GetBatchAsync(ObjectId.Empty, ObjectId.Empty))
                .ReturnsAsync(new[] { new ClamObservationVerbatim
                {
                    DyntaxaTaxonId = 0
                } });

            _areaHelper.Setup(r => r.AddAreaDataToProcessedObservations(It.IsAny<IEnumerable<ProcessedObservation>>()));

            _processedObservationRepositoryMock.Setup(r => r.AddManyAsync(It.IsAny<ICollection<ProcessedObservation>>()))
                .ReturnsAsync(1);

            var taxa = new Dictionary<int, ProcessedTaxon>
            {
                { 0, new ProcessedTaxon { Id = 0, TaxonId = "taxon:0", ScientificName = "Biota" } }
            };

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var clamPortalProcessor = new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);

            var result = await clamPortalProcessor.ProcessAsync(taxa, JobCancellationToken.Null);
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
            var clamPortalProcessor = new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);

            var result = await clamPortalProcessor.ProcessAsync(null, JobCancellationToken.Null);
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
            _clamObservationVerbatimRepositoryMock.Setup(r => r.GetBatchAsync(ObjectId.Empty, ObjectId.Empty))
                .ThrowsAsync(new Exception("Failed"));
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var clamPortalProcessor = new ClamPortalObservationProcessor(
                _clamObservationVerbatimRepositoryMock.Object,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);

            var result = await clamPortalProcessor.ProcessAsync(null, JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Failed);
        }
    }
}