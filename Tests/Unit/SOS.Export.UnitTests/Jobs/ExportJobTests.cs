using System;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using SOS.Export.Jobs;
using SOS.Export.Managers.Interfaces;
using SOS.Lib.Models.Search;
using Xunit;

namespace SOS.Export.UnitTests.Jobs
{
    /// <summary>
    /// Tests for observation manager
    /// </summary>
    public class ExportJobTests
    {
        private readonly Mock<IObservationManager> _observationManager;
        private readonly Mock<ILogger<ExportJob>> _loggerMock;

        /// <summary>
        /// Return object to be tested
        /// </summary>
        private ExportJob TestObject => new ExportJob(
            _observationManager.Object,
            _loggerMock.Object);

        /// <summary>
        /// Constructor
        /// </summary>
        public ExportJobTests()
        {
            _observationManager = new Mock<IObservationManager>();
            _loggerMock = new Mock<ILogger<ExportJob>>();
        }

        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        public void ConstructorTest()
        {
            TestObject.Should().NotBeNull();

            Action create = () => new ExportJob(
                null,
                _loggerMock.Object);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("observationManager");

            create = () => new ExportJob(
                _observationManager.Object,
                null);
            create.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        /// <summary>
        /// Make a successful test of export
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
                    .ExportDWCAsync(It.IsAny<ExportFilter>(), It.IsAny<string>(), JobCancellationToken.Null)
                )
                .ReturnsAsync(true);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

           var result = await observationManager.RunAsync(new ExportFilter(), null, JobCancellationToken.Null);

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            result.Should().BeTrue();
        }


        /// <summary>
        /// Test run fail
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
                    .ExportDWCAsync(It.IsAny<ExportFilter>(), It.IsAny<string>(), JobCancellationToken.Null)
                )
                .ReturnsAsync(false);

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

            Func<Task> act = async () => { await observationManager.RunAsync(new ExportFilter(), null, JobCancellationToken.Null); };

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            await act.Should().ThrowAsync<Exception>();
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
                    .ExportDWCAsync(It.IsAny<ExportFilter>(), It.IsAny<string>(), JobCancellationToken.Null)
                )
                .Throws(new Exception());

            //-----------------------------------------------------------------------------------------------------------
            // Act
            //-----------------------------------------------------------------------------------------------------------
            var observationManager = TestObject;

            Func<Task> act = async () => { await observationManager.RunAsync(new ExportFilter(), null, JobCancellationToken.Null); };

            //-----------------------------------------------------------------------------------------------------------
            // Assert
            //-----------------------------------------------------------------------------------------------------------
            await act.Should().ThrowAsync<Exception>();
        }
    }
}
