﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SOS.Lib.Enums;
using SOS.Lib.Extensions;
using SOS.Lib.Models.DarwinCore;
using SOS.Lib.Models.Interfaces;
using SOS.Lib.Models.Processed.Sighting;
using SOS.Lib.Models.TaxonTree;
using SOS.Process.Extensions;
using SOS.Process.Jobs.Interfaces;
using SOS.Process.Repositories.Destination.Interfaces;
using SOS.Process.Repositories.Source.Interfaces;

namespace SOS.Process.Jobs
{
    public class ProcessTaxaJob : IProcessTaxaJob
    {
        private readonly ITaxonVerbatimRepository _taxonVerbatimRepository;
        private readonly ITaxonProcessedRepository _taxonProcessedRepository;
        private readonly ILogger<ProcessTaxaJob> _logger;

        public ProcessTaxaJob(
            ITaxonVerbatimRepository taxonVerbatimRepository,
            ITaxonProcessedRepository taxonProcessedRepository,
            ILogger<ProcessTaxaJob> logger)
        {
            _taxonVerbatimRepository = taxonVerbatimRepository ?? throw new ArgumentNullException(nameof(taxonVerbatimRepository));
            _taxonProcessedRepository = taxonProcessedRepository ?? throw new ArgumentNullException(nameof(taxonProcessedRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> RunAsync()
        {
            var dwcTaxa = await GetDarwinCoreTaxaAsync();
            if (!dwcTaxa?.Any() ?? true)
            {
                _logger.LogDebug("Failed to get taxa");
                return false;
            }

            // Process taxa
            var taxa = dwcTaxa.Select(m => m.ToProcessedTaxon()).ToList();
            CalculateHigherClassificationField(taxa);

            _logger.LogDebug("Start deleting data from inactive instance");
            if (!await _taxonProcessedRepository.DeleteCollectionAsync())
            {
                _logger.LogError("Failed to delete ProcessedTaxon data");
                return false;
            }

            var result = await _taxonProcessedRepository.AddManyAsync(taxa);
            return result;
        }

        private async Task<IEnumerable<DarwinCoreTaxon>> GetDarwinCoreTaxaAsync()
        {
            var skip = 0;
            var tmpTaxa = await _taxonVerbatimRepository.GetBatchAsync(skip);
            var taxa = new List<DarwinCoreTaxon>();

            while (tmpTaxa?.Any() ?? false)
            {
                taxa.AddRange(tmpTaxa);
                skip += tmpTaxa.Count();
                tmpTaxa = await _taxonVerbatimRepository.GetBatchAsync(skip);
            }

            return taxa;
        }

        /// <summary>
        /// Calculate higher classification tree by creating a taxon tree and iterate
        /// each nodes parents.
        /// </summary>
        /// <param name="taxa"></param>
        private void CalculateHigherClassificationField(IEnumerable<ProcessedTaxon> taxa)
        {
            var taxonTree = TaxonTreeFactory.CreateTaxonTree(taxa);
            var taxonById = taxa.ToDictionary(m => m.Id, m => m);
            foreach (var treeNode in taxonTree.TreeNodeById.Values)
            {
                var parentNames = treeNode.AsParentsNodeIterator().Select(m => m.ScientificName);
                var reversedParentNames = parentNames.Reverse();
                string higherClassification = string.Join(" | ", reversedParentNames);
                taxonById[treeNode.TaxonId].HigherClassification = higherClassification;
            }
        }
    }
}