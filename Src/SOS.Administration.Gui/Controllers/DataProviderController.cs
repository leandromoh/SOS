﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SOS.Administration.Gui.Models;
using SOS.Lib.Configuration.Shared;

namespace SOS.Administration.Gui.Controllers
{
  
    [ApiController]
    [Route("[controller]")]
    public class DataProviderController : ControllerBase
    {       
        private readonly ILogger<DataProviderController> _logger;
        private MongoClient _client;

        public DataProviderController(ILogger<DataProviderController> logger, IOptionsMonitor<MongoDbConfiguration> mongoDbSettings)
        {
            _logger = logger;
            _client = new MongoClient(mongoDbSettings.CurrentValue.GetMongoDbSettings());
        }

        [HttpGet]
        [Route("")]
        public IEnumerable<DataProviderDto> Get()
        {
            var database = _client.GetDatabase("sos");            
            var collection = database.GetCollection<DataProviderDto>("DataProvider");
            var providers = collection.Find(new BsonDocument());
            return providers.ToList();          
       }
    }
}