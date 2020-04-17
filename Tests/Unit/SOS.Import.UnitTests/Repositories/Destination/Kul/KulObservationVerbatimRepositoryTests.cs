using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.MongoDb.Interfaces;
using SOS.Import.Repositories.Destination.Kul;
using Xunit;

namespace SOS.Import.UnitTests.Repositories.Destination.Kul
{
    /// <summary>
    /// Meta data repository tests
    /// </summary>
    public class KulObservationVerbatimRepositoryTests
    {
        private readonly Mock<IImportClient> _importClient;
        private readonly Mock<ILogger<KulObservationVerbatimRepository>> _loggerMock;

        private KulObservationVerbatimRepository TestObject => new KulObservationVerbatimRepository(
            _importClient.Object,
            _loggerMock.Object);

        /// <summary>
        /// Constructor
        /// </summary>
        public KulObservationVerbatimRepositoryTests()
        {
            _importClient = new Mock<IImportClient>();
            _loggerMock = new Mock<ILogger<KulObservationVerbatimRepository>>();
        }

        /// <summary>
        /// Test the constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new KulObservationVerbatimRepository(
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("importClient");

            create = () => new KulObservationVerbatimRepository(
                _importClient.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

    }
}