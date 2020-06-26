﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SOS.Import.Services.Interfaces;
using SOS.Lib.Configuration.Import;

namespace SOS.Import.Services
{
    public class FishDataObservationService : IFishDataObservationService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly FishDataServiceConfiguration _fishDataServiceConfiguration;
        private readonly ILogger<FishDataObservationService> _logger;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="speciesObservationChangeServiceClient"></param>
        /// <param name="fishDataServiceConfiguration"></param>
        /// <param name="logger"></param>
        public FishDataObservationService(
            IHttpClientService httpClientService,
            FishDataServiceConfiguration fishDataServiceConfiguration,
            ILogger<FishDataObservationService> logger)
        {
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _fishDataServiceConfiguration = fishDataServiceConfiguration ??
                                       throw new ArgumentNullException(nameof(fishDataServiceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<XDocument> GetAsync(long changeId)
        {
            try
            {
                var xmlStream = await _httpClientService.GetFileStreamAsync(
                    new Uri($"{_fishDataServiceConfiguration.BaseAddress}/api/v1/FishDataSpeciesObservation/?token={_fishDataServiceConfiguration.Token}" +
                            $"&changedFrom={ new DateTime(_fishDataServiceConfiguration.StartHarvestYear,1,1).ToString("yyyy-MM-dd")  }" +
                            $"&isChangedFromSpecified=false" +
                            $"&changedTo={ DateTime.Now.ToString("yyyy-MM-dd") }" +
                            $"&isChangedToSpecified=false" +
                            $"&changeId={changeId}" +
                            $"&isChangedIdSpecified=true" +
                            $"&maxReturnedChanges={_fishDataServiceConfiguration.MaxReturnedChangesInOnePage}"),
                    new Dictionary<string, string>(new[]
                        {
                            new KeyValuePair<string, string>("Accept", _fishDataServiceConfiguration.AcceptHeaderContentType),
                        }
                        )
                    );

                xmlStream.Seek(0, SeekOrigin.Begin);

                var xDocument = await XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None);

                return xDocument;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get data from Fish data", e);
                return null;
            }
        }
    }
}