using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using SOS.Lib.Enums;
using SOS.Lib.Models.Processed.Observation;
using SOS.Lib.Models.Shared;
using SOS.Lib.Models.Verbatim.Sers;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Process.Helpers.Interfaces;
using SOS.Process.Processors.Sers;
using SOS.Process.Repositories.Destination.Interfaces;
using SOS.Process.Repositories.Source.Interfaces;
using Xunit;

namespace SOS.Process.UnitTests.Processors
{
    /// <summary>
    /// Tests for Clam Portal processor
    /// </summary>
    public class SersObservationProcessorTests
    {
        private readonly Mock<ISersObservationVerbatimRepository> _sersObservationVerbatimRepositoryMock;
        private readonly Mock<IAreaHelper> _areaHelper;
        private readonly Mock<IProcessedObservationRepository> _processedObservationRepositoryMock;
        private readonly Mock<IFieldMappingResolverHelper> _fieldMappingResolverHelperMock;
        private readonly Mock<ILogger<SersObservationProcessor>> _loggerMock;

        private SersObservationProcessor TestObject => new SersObservationProcessor(
            _sersObservationVerbatimRepositoryMock.Object,
            _areaHelper.Object,
            _processedObservationRepositoryMock.Object,
            _fieldMappingResolverHelperMock.Object,
            _loggerMock.Object);

        /// <summary>
        /// Constructor
        /// </summary>
        public SersObservationProcessorTests()
        {
            _sersObservationVerbatimRepositoryMock = new Mock<ISersObservationVerbatimRepository>();
            _areaHelper = new Mock<IAreaHelper>();
            _processedObservationRepositoryMock = new Mock<IProcessedObservationRepository>();
            _fieldMappingResolverHelperMock = new Mock<IFieldMappingResolverHelper>();
            _loggerMock = new Mock<ILogger<SersObservationProcessor>>();
        }

        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new SersObservationProcessor(
                null,
                _areaHelper.Object,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("sersObservationVerbatimRepository");


            create = () => new SersObservationProcessor(
                _sersObservationVerbatimRepositoryMock.Object,
                null,
                _processedObservationRepositoryMock.Object,
                _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("areaHelper");

            create = () => new SersObservationProcessor(
                 _sersObservationVerbatimRepositoryMock.Object,
                 _areaHelper.Object,
                null,
                 _fieldMappingResolverHelperMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("processedObservationRepository");

            create = () => new SersObservationProcessor(
                _sersObservationVerbatimRepositoryMock.Object,
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
            var mockCursor = new Mock<IAsyncCursor<SersObservationVerbatim>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<SersObservationVerbatim>()); 
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true))
                .Returns(Task.FromResult(false));

            _sersObservationVerbatimRepositoryMock.Setup(r => r.GetAllByCursorAsync())
                .ReturnsAsync(mockCursor.Object);

            _areaHelper.Setup(r => r.AddAreaDataToProcessedObservations(It.IsAny<IEnumerable<ProcessedObservation>>()));

            _processedObservationRepositoryMock.Setup(r => r.DeleteProviderDataAsync(It.IsAny<DataProvider>()))
                .ReturnsAsync(true);

            _processedObservationRepositoryMock.Setup(r => r.AddManyAsync(It.IsAny<ICollection<ProcessedObservation>>()))
                .ReturnsAsync(1);

            var taxa = new Dictionary<int, ProcessedTaxon>
            {
                { 0, new ProcessedTaxon { Id = 0, TaxonId = "taxon:0", ScientificName = "Biota" } }
            };

            var dataProvider = CreateDataProvider();

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.ProcessAsync(dataProvider, taxa, JobCancellationToken.Null);
            
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
            var dataProvider = CreateDataProvider();

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.ProcessAsync(dataProvider, null, JobCancellationToken.Null);
            
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
            var dataProvider = CreateDataProvider();
            _sersObservationVerbatimRepositoryMock.Setup(r => r.GetAllByCursorAsync())
                .ThrowsAsync(new Exception("Failed"));
            
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.ProcessAsync(dataProvider, null, JobCancellationToken.Null);
            
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Failed);
        }

        private DataProvider CreateDataProvider()
        {
            return new DataProvider
            {
                Name = "SERS",
                Type = DataSet.SersObservations
            };
        }
    }
}
