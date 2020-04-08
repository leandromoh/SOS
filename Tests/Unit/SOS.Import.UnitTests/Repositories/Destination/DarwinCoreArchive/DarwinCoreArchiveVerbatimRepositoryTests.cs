using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.MongoDb.Interfaces;
using SOS.Import.Repositories.Destination.DarwinCoreArchive;
using Xunit;

namespace SOS.Import.UnitTests.Repositories.Destination.DarwinCoreArchive
{
    /// <summary>
    /// Meta data repository tests
    /// </summary>
    public class DarwinCoreArchiveVerbatimRepositoryTests
    {
        private readonly Mock<IImportClient> _importClient;
        private readonly Mock<ILogger<DarwinCoreArchiveVerbatimRepository>> _loggerMock;

        private DarwinCoreArchiveVerbatimRepository TestObject => new DarwinCoreArchiveVerbatimRepository(
            _importClient.Object,
            _loggerMock.Object);

        /// <summary>
        /// Constructor
        /// </summary>
        public DarwinCoreArchiveVerbatimRepositoryTests()
        {
            _importClient = new Mock<IImportClient>();
            _loggerMock = new Mock<ILogger<DarwinCoreArchiveVerbatimRepository>>();
        }

        /// <summary>
        /// Test the constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new DarwinCoreArchiveVerbatimRepository(
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("importClient");

            create = () => new DarwinCoreArchiveVerbatimRepository(
                _importClient.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

    }
}
