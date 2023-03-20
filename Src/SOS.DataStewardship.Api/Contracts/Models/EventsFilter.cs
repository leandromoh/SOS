using SOS.DataStewardship.Api.Contracts.Enums;
using SOS.Lib.Swagger;
using Swashbuckle.AspNetCore.Annotations;

namespace SOS.DataStewardship.Api.Contracts.Models
{    
    [SwaggerSchema("Events search filter")]
    public class EventsFilter
    {        
        [SwaggerSchema("Export mode")]
        public ExportMode ExportMode { get; set; }
        
        [SwaggerSchema("DatasetIds filter")]
        public List<string> DatasetIds { get; set; }

        [SwaggerExclude]
        public List<string> DatasetList { get; set; }

        [SwaggerSchema("EventIds filter")]
        public List<string> EventIds { get; set; }

        [SwaggerSchema("Date filter")]
        public DateFilter DateFilter { get; set; }

        [SwaggerExclude]
        public DateFilter Datum { get; set; }

        [SwaggerSchema("Taxon filter")]
        public TaxonFilter Taxon { get; set; }

        [SwaggerSchema("Area filter")]
        public GeographicsFilter Area { get; set; }
    }
}
