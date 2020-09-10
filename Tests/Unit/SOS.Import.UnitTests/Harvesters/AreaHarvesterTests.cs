using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite.Geometries;
using SOS.Import.Entities.Artportalen;
using SOS.Import.Harvesters;
using SOS.Import.Repositories.Destination.Area.Interfaces;
using SOS.Import.Repositories.Destination.Artportalen.Interfaces;
using SOS.Import.Repositories.Source.Artportalen.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.Shared;
using SOS.Process.Repositories.Source.Interfaces;
using Xunit;

namespace SOS.Import.UnitTests.Harvesters
{
    /// <summary>
    ///     Tests for area harvester
    /// </summary>
    public class AreaHarvesterTests
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public AreaHarvesterTests()
        {
            _areaRepositoryMock = new Mock<IAreaRepository>();
            _areaProcessedRepository = new Mock<IAreaProcessedRepository>();
            _loggerMock = new Mock<ILogger<AreaHarvester>>();
        }

        private readonly Mock<IAreaRepository> _areaRepositoryMock;
        private readonly Mock<IAreaProcessedRepository> _areaProcessedRepository;
        private readonly Mock<ILogger<AreaHarvester>> _loggerMock;

        private AreaHarvester TestObject => new AreaHarvester(
            _areaRepositoryMock.Object,
            _areaProcessedRepository.Object,
            _loggerMock.Object);

        /// <summary>
        ///     Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new AreaHarvester(
                null,
                _areaProcessedRepository.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("areaRepository");

            create = () => new AreaHarvester(
                _areaRepositoryMock.Object,
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("areaProcessedRepository");

            create = () => new AreaHarvester(
                _areaRepositoryMock.Object,
                _areaProcessedRepository.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        /// <summary>
        ///     Test aggregation fail
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HarvestAreasAsyncFail()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------


            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.HarvestAreasAsync();
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Failed);
        }

        /// <summary>
        ///     Make a successful test of aggregation
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HarvestAreasAsyncSuccess()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _areaRepositoryMock.Setup(mdr => mdr.GetAsync())
                .ReturnsAsync(new[] {new AreaEntity {Id = 1, Name = "Sverige"}});

            _areaProcessedRepository.Setup(tr => tr.DeleteCollectionAsync())
                .ReturnsAsync(true);
            _areaProcessedRepository.Setup(tr => tr.AddCollectionAsync())
                .ReturnsAsync(true);
            _areaProcessedRepository.Setup(tr => tr.AddManyAsync(It.IsAny<IEnumerable<Area>>()))
                .ReturnsAsync(true);
            _areaProcessedRepository.Setup(tr => tr.StoreGeometriesAsync(It.IsAny<IDictionary<int, Geometry>>()))
                .ReturnsAsync(true);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.HarvestAreasAsync();
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Status.Should().Be(RunStatus.Success);
        }
    }
}