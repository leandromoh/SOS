using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Lib.Database.Interfaces;
using SOS.Lib.Repositories.Verbatim;
using Xunit;

namespace SOS.Import.UnitTests.Repositories.Destination.Artportalen
{
    /// <summary>
    ///     Meta data repository tests
    /// </summary>
    public class SightingVerbatimRepositoryTests
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public SightingVerbatimRepositoryTests()
        {
            _importClient = new Mock<IVerbatimClient>();
            _loggerMock = new Mock<ILogger<ArtportalenVerbatimRepository>>();
        }

        private readonly Mock<IVerbatimClient> _importClient;
        private readonly Mock<ILogger<ArtportalenVerbatimRepository>> _loggerMock;

        private ArtportalenVerbatimRepository TestObject => new ArtportalenVerbatimRepository(
            _importClient.Object,
            _loggerMock.Object);

        /// <summary>
        ///     Test the constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new ArtportalenVerbatimRepository(
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("importClient");

            create = () => new ArtportalenVerbatimRepository(
                _importClient.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }
    }
}