﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Import.Harvesters.Interfaces;
using SOS.Import.Jobs;
using SOS.Lib.Enums;
using SOS.Lib.Models.Verbatim.Shared;
using SOS.Lib.Repositories.Verbatim.Interfaces;
using Xunit;

namespace SOS.Import.UnitTests.Jobs
{
    public class VocabulariesImportJobTests
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public VocabulariesImportJobTests()
        {
            _vocabularyHarvesterMock = new Mock<IVocabularyHarvester>();
            _harvestInfoRepositoryMock = new Mock<IHarvestInfoRepository>();
            _loggerMock = new Mock<ILogger<VocabulariesImportJob>>();
        }

        private readonly Mock<IVocabularyHarvester> _vocabularyHarvesterMock;
        private readonly Mock<IHarvestInfoRepository> _harvestInfoRepositoryMock;
        private readonly Mock<ILogger<VocabulariesImportJob>> _loggerMock;

        private VocabulariesImportJob TestObject => new VocabulariesImportJob(
            _vocabularyHarvesterMock.Object,
            _harvestInfoRepositoryMock.Object,
            _loggerMock.Object);

        /// <summary>
        ///     Harvest job throw exception
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddDataProviderException()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _vocabularyHarvesterMock.Setup(ts => ts.HarvestAsync())
                .Throws<Exception>();
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            Func<Task> act = async () => { await TestObject.RunAsync(); };
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            await act.Should().ThrowAsync<Exception>();
        }

        /// <summary>
        ///     Fail to run harvest job
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddDataProviderFail()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _vocabularyHarvesterMock.Setup(ts => ts.HarvestAsync())
                .ReturnsAsync(new HarvestInfo("id", DataProviderType.Taxa, DateTime.Now) {Status = RunStatus.Failed});

            _harvestInfoRepositoryMock.Setup(ts => ts.AddOrUpdateAsync(It.IsAny<HarvestInfo>()));
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            Func<Task> act = async () => { await TestObject.RunAsync(); };
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            await act.Should().ThrowAsync<Exception>();
        }

        /// <summary>
        ///     Test constructor
        /// </summary>
        [Fact]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new VocabulariesImportJob(
                null,
                _harvestInfoRepositoryMock.Object,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fieldMappingHarvester");

            create = () => new VocabulariesImportJob(
                _vocabularyHarvesterMock.Object,
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("harvestInfoRepository");

            create = () => new VocabulariesImportJob(
                _vocabularyHarvesterMock.Object,
                _harvestInfoRepositoryMock.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        /// <summary>
        ///     Run harvest job successfully
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task RunAsyncSuccess()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _vocabularyHarvesterMock.Setup(ts => ts.HarvestAsync())
                .ReturnsAsync(new HarvestInfo("id", DataProviderType.Taxa, DateTime.Now) {Status = RunStatus.Success, Count = 1 });

            _harvestInfoRepositoryMock.Setup(ts => ts.AddOrUpdateAsync(It.IsAny<HarvestInfo>()));
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var result = await TestObject.RunAsync();
            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------

            result.Should().BeTrue();
        }
    }
}