using System;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Export.Jobs;
using SOS.Export.Managers.Interfaces;
using SOS.Lib.Enums;
using SOS.Lib.Models.Export;
using SOS.Lib.Models.Search;
using SOS.Lib.Repositories.Processed.Interfaces;
using Xunit;

namespace SOS.Export.UnitTests.Jobs
{
    /// <summary>
    ///     Tests for observation manager
    /// </summary>
    public class ExportAndSendJobTests
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public ExportAndSendJobTests()
        {
            _observationManager = new Mock<IObservationManager>();
            _userExportRepository = new Mock<IUserExportRepository>();
            _loggerMock = new Mock<ILogger<ExportAndSendJob>>();
        }

        private readonly Mock<IObservationManager> _observationManager;
        private readonly Mock<IUserExportRepository> _userExportRepository;
        private readonly Mock<ILogger<ExportAndSendJob>> _loggerMock;

        /// <summary>
        ///     Return object to be tested
        /// </summary>
        private ExportAndSendJob TestObject => new ExportAndSendJob(
            _observationManager.Object,
            _userExportRepository.Object,
            _loggerMock.Object);

        /// <summary>
        ///     Test run fail
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task RunAsyncFail()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _observationManager
                .Setup(blss => blss
                    .ExportAndSendAsync(It.IsAny<SearchFilter>(), It.IsAny<string>(), "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, JobCancellationToken.Null)
                )
                .ReturnsAsync(false);
            _userExportRepository.Setup(uer => uer.GetAsync(It.IsAny<int>())).ReturnsAsync(new UserExport());
            _userExportRepository.Setup(uer => uer.AddOrUpdateAsync(It.IsAny<UserExport>())).ReturnsAsync(true);
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

            Func<Task> act = async () =>
            {
                await observationManager.RunAsync(new SearchFilter(), 0, null, "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, null, JobCancellationToken.Null);
            };

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            await act.Should().ThrowAsync<Exception>();
        }

        /// <summary>
        ///     Make a successful test of export
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task RunAsyncSucess()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _observationManager
                .Setup(blss => blss
                    .ExportAndSendAsync(It.IsAny<SearchFilter>(), It.IsAny<string>(), "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, JobCancellationToken.Null)
                )
                .ReturnsAsync(true);

            _userExportRepository.Setup(uer => uer.GetAsync(It.IsAny<int>())).ReturnsAsync(new UserExport());
            _userExportRepository.Setup(uer => uer.AddOrUpdateAsync(It.IsAny<UserExport>())).ReturnsAsync(true);
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

            var result = await observationManager.RunAsync(new SearchFilter(), 0, null, "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, null, JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task RunAsyncThrows()
        {
            // -----------------------------------------------------------------------------------------------------------
            // Arrange
            //-----------------------------------------------------------------------------------------------------------
            _observationManager
                .Setup(blss => blss
                    .ExportAndSendAsync(It.IsAny<SearchFilter>(), It.IsAny<string>(), "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, JobCancellationToken.Null)
                )
                .Throws(new Exception());
            _userExportRepository.Setup(uer => uer.GetAsync(It.IsAny<int>())).ReturnsAsync(new UserExport());
            _userExportRepository.Setup(uer => uer.AddOrUpdateAsync(It.IsAny<UserExport>())).ReturnsAsync(true);
            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

            Func<Task> act = async () =>
            {
                await observationManager.RunAsync(new SearchFilter(), 0, null, "", ExportFormat.DwC, "en-GB", false, OutputFieldSet.All, PropertyLabelType.PropertyName, false, null, JobCancellationToken.Null);
            };

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            await act.Should().ThrowAsync<Exception>();
        }
    }
}