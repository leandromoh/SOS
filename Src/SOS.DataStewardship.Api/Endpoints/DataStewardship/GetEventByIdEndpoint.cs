﻿using SOS.DataStewardship.Api.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using SOS.DataStewardship.Api.Managers.Interfaces;
using SOS.DataStewardship.Api.Models.Enums;
using SOS.DataStewardship.Api.Models;
using SOS.DataStewardship.Api.Extensions;

namespace SOS.DataStewardship.Api.Endpoints.DataStewardship;

public class GetEventByIdEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(WebApplication app)
    {
        app.MapGet("/datastewardship/events/{id}", GetEventByIdAsync)
            .Produces<EventModel>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            //.Produces<List<FluentValidation.Results.ValidationFailure>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }
    
    [SwaggerOperation(
        Description = "Get event by Id. Example: urn:lsid:swedishlifewatch.se:dataprovider:Artportalen:event:10002293427000658739",
        OperationId = "GetEventById",
        Tags = new[] { "DataStewardship" })]
    [SwaggerResponse(404, "Not Found - The requested event doesn't exist")]    
    private async Task<IResult> GetEventByIdAsync(IDataStewardshipManager dataStewardshipManager,
        [FromRoute, SwaggerParameter("The event id", Required = true)] string id,
        [FromQuery, SwaggerParameter("The export mode")] ExportMode exportMode = ExportMode.Json)
    {        
        var eventModel = await dataStewardshipManager.GetEventByIdAsync(id);
        if (eventModel == null) return Results.NotFound();

        return exportMode.Equals(ExportMode.Csv) ? Results.File(eventModel.ToCsv(), "text/tab-separated-values", "dataset.csv") : Results.Ok(eventModel);       
    }
}