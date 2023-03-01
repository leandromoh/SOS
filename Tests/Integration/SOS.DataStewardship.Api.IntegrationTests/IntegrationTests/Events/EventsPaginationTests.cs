namespace SOS.DataStewardship.Api.IntegrationTests.IntegrationTests.Events;

[Collection(Constants.IntegrationTestsCollectionName)]
public class EventsPaginationTests : TestBase
{
    public EventsPaginationTests(TestFixture testFixture, ITestOutputHelper output) : base(testFixture, output)
    {
    }

    [Fact]
    public async Task EventsBySearch_ReturnsAllEvents_WhenPaginatingAllRecords()
    {
        // Arrange
        var testData = TestData.GetTestData();
        await ProcessFixture.AddDataToElasticsearchAsync((testData.events, testData.observations));
        var searchFilter = new EventsFilter();
        var eventModels = new List<EventModel>();
        int take = 2;

        // Act
        for (int skip=0; skip < testData.events.Count(); skip += take)
        {
            var pageResult = await ApiClient.PostAndReturnAsJsonAsync<PagedResult<EventModel>, EventsFilter>(
                $"datastewardship/events?skip={skip}&take={take}", searchFilter, jsonSerializerOptions);
            eventModels.AddRange(pageResult.Records);
        }

        // Assert
        var eventIds = testData.events.Select(m => m.EventId);
        var uniqueEventIds = eventModels.Select(m => m.EventID).Distinct();
        uniqueEventIds.Should().BeEquivalentTo(eventIds);
    }


    [Fact]
    public async Task EventsBySearch_ReturnsCorrectPagingMetadata_GivenValidInput()
    {
        // Arrange
        var testData = TestData.GetTestData();
        await ProcessFixture.AddDataToElasticsearchAsync((testData.events, testData.observations));
        var searchFilter = new EventsFilter();
        int skip = 5;
        int take = 2;

        // Act        
        var pageResult = await ApiClient.PostAndReturnAsJsonAsync<PagedResult<EventModel>, EventsFilter>(
            $"datastewardship/events?skip={skip}&take={take}", searchFilter, jsonSerializerOptions);
        
        // Assert
        pageResult.TotalCount.Should().Be(testData.events.Count());
        pageResult.Take.Should().Be(take);
        pageResult.Count.Should().Be(take);
        pageResult.Skip.Should().Be(skip);
    }
    

    [Fact]
    public async Task EventsBySearch_ReturnsNoRecords_GivenOutOfRangeSkipParameter()
    {
        // Arrange
        var testData = TestData.GetTestData();
        await ProcessFixture.AddDataToElasticsearchAsync((testData.events, testData.observations));
        var searchFilter = new EventsFilter();
        int skip = testData.events.Count();
        int take = 2;

        // Act
        var pageResult = await ApiClient.PostAndReturnAsJsonAsync<PagedResult<EventModel>, EventsFilter>(
            $"datastewardship/events?skip={skip}&take={take}", searchFilter, jsonSerializerOptions);

        // Assert
        pageResult.TotalCount.Should().Be(testData.events.Count());
        pageResult.Take.Should().Be(take);
        pageResult.Count.Should().Be(0);
        pageResult.Skip.Should().Be(skip);
    }

    [Fact]
    public async Task EventsBySearch_ReturnsBadRequest_GivenInvalidSkipAndTake()
    {
        // Arrange
        var testData = TestData.GetTestData();
        await ProcessFixture.AddDataToElasticsearchAsync((testData.events, testData.observations));        
        
        var searchFilter = new EventsFilter();
        int skipNegative = -1;
        int skipTooLarge = 1000000;
        int skip = 2;
        int take = 2;
        int takeNegative = -1;
        int takeTooLarge = 1000000;

        // Act
        var responseSkipNegative = await ApiClient.PostAsJsonAsync(
            $"datastewardship/events?skip={skipNegative}&take={take}", searchFilter, jsonSerializerOptions);

        var responseSkipTooLarge = await ApiClient.PostAsJsonAsync(
            $"datastewardship/events?skip={skipTooLarge}&take={take}", searchFilter, jsonSerializerOptions);

        var responseTakeNegative = await ApiClient.PostAsJsonAsync(
            $"datastewardship/events?skip={skip}&take={takeNegative}", searchFilter, jsonSerializerOptions);

        var responseTakeTooLarge = await ApiClient.PostAsJsonAsync(
            $"datastewardship/events?skip={skip}&take={takeTooLarge}", searchFilter, jsonSerializerOptions);

        // Assert
        responseSkipNegative.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        responseSkipTooLarge.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        responseTakeNegative.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        responseTakeTooLarge.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}