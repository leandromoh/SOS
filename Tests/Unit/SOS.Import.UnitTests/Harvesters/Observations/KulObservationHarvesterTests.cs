﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using KulService;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.Harvesters.Observations;
using SOS.Import.Repositories.Destination.Kul.Interfaces;
using SOS.Import.Services.Interfaces;
using SOS.Lib.Configuration.Import;
using SOS.Lib.Enums;
using SOS.Lib.Models.Verbatim.Kul;
using Xunit;

namespace SOS.Import.UnitTests.Harvesters.Observations
{
    public class KulObservationHarvesterTests
    {
        private readonly Mock<IKulObservationVerbatimRepository> _kulObservationVerbatimRepositoryMock;
        private readonly Mock<IKulObservationService> _kulObservationServiceMock;
        private readonly KulServiceConfiguration _kulServiceConfiguration;
        private readonly Mock<ILogger<KulObservationHarvester>> _loggerMock;

        private KulObservationHarvester TestObject => new KulObservationHarvester(
            _kulObservationServiceMock.Object,
            _kulObservationVerbatimRepositoryMock.Object,
            _kulServiceConfiguration,
            _loggerMock.Object);

        /// <summary>
        /// Constructor
        /// </summary>
        public KulObservationHarvesterTests()
        {
            _kulObservationVerbatimRepositoryMock = new Mock<IKulObservationVerbatimRepository>();
            _kulObservationServiceMock = new Mock<IKulObservationService>();
            _kulServiceConfiguration = new KulServiceConfiguration{StartHarvestYear = DateTime.Now.Year};
            _loggerMock = new Mock<ILogger<KulObservationHarvester>>();
        }

        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new KulObservationHarvester(
                null,
                _kulObservationVerbatimRepositoryMock.Object,
                _kulServiceConfiguration,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("kulObservationService");

            create = () => new KulObservationHarvester(
                _kulObservationServiceMock.Object,
               null,
                _kulServiceConfiguration,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("kulObservationVerbatimRepository");

            create = () => new KulObservationHarvester(
                _kulObservationServiceMock.Object,
                _kulObservationVerbatimRepositoryMock.Object,
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("kulServiceConfiguration");

            create = () => new KulObservationHarvester(
                _kulObservationServiceMock.Object,
                _kulObservationVerbatimRepositoryMock.Object,
                _kulServiceConfiguration,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        /// <summary>
        /// Make a successful kuls harvest
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HarvestKulsAsyncSuccess()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _kulObservationServiceMock.Setup(cts => cts.GetAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<WebSpeciesObservation>());

            _kulObservationVerbatimRepositoryMock.Setup(tr => tr.DeleteCollectionAsync())
                .ReturnsAsync(true);
            _kulObservationVerbatimRepositoryMock.Setup(tr => tr.AddCollectionAsync())
                .ReturnsAsync(true);
            _kulObservationVerbatimRepositoryMock.Setup(tr => tr.AddManyAsync(It.IsAny<IEnumerable<KulObservationVerbatim>>()))
                .ReturnsAsync(true);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.HarvestObservationsAsync(JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Success);
        }

        /// <summary>
        /// Test aggregation fail
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HarvestKulsAsyncFail()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _kulObservationServiceMock.Setup(cts => cts.GetAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                 .ThrowsAsync(new Exception("Fail"));
            
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.HarvestObservationsAsync(JobCancellationToken.Null);
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Status.Should().Be(RunStatus.Failed);
        }
    }
}