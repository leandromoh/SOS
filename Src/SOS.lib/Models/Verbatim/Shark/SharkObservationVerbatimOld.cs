﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SOS.Lib.Models.Interfaces;

namespace SOS.Lib.Models.Verbatim.Shark
{
    /// <summary>
    /// Verbatim from Shark
    /// </summary>
    public class SharkObservationVerbatimOld : IEntity<ObjectId>
    {
        /// <summary>
        /// Additional sampling
        /// </summary>
        public string AdditionalSampling { get; set; }

        /// <summary>
        /// Air temperature in celcius
        /// </summary>
        public string AirTemperatureDegc { get; set; }

        /// <summary>
        /// Air pressure in Hpa
        /// </summary>
        public string AirPressureHpa { get; set; }

        /// <summary>Ait temperature wet degrees
        /// 
        /// </summary>
        public string AirTemperatureWetDegc { get; set; }

        /// <summary>
        /// Analysed by
        /// </summary>
        public string AnalysedBy { get; set; }

        /// <summary>
        /// Analysed volume in cm3
        /// </summary>
        public string AnalysedVolumeCm3 { get; set; }

        /// <summary>
        /// Analysis date
        /// </summary>
        public string AnalysisDate { get; set; }

        /// <summary>
        /// Analytical laboratory accreditated
        /// </summary>
        public string AnalyticalLaboratoryAccreditated { get; set; }

        /// <summary>
        /// Analytical laboratory name
        /// </summary>
        public string AnalyticalLaboratoryNameSv { get; set; }

        /// <summary>
        /// Analysis method code
        /// </summary>
        public string AnalysisMethodCode { get; set; }

        /// <summary>
        /// Analysis range
        /// </summary>
        public string AnalysisRange { get; set; }

        /// <summary>
        /// Calc by dc
        /// </summary>
        public string CalcByDc { get; set; }

        /// <summary>
        /// Check status
        /// </summary>
        public string CheckStatusSv { get; set; }

        /// <summary>
        /// Cloud observation code
        /// </summary>
        public string CloudObservationCode { get; set; }

        /// <summary>
        /// Coefficient
        /// </summary>
        public string Coefficient { get; set; }

        /// <summary>
        /// Counted portions
        /// </summary>
        public string CountedPortions { get; set; }

        /// <summary>
        /// Data checked by
        /// </summary>
        public string DataCheckedBySv { get; set; }

        /// <summary>
        /// Data holding centre
        /// </summary>
        public string DataHoldingCentre { get; set; }

        /// <summary>
        /// Name of data set
        /// </summary>
        public string DatasetName { get; set; }

        /// <summary>
        /// Name of data set file
        /// </summary>
        public string DatasetFileName { get; set; }

        /// <summary>
        /// Delivery data type
        /// </summary>
        public string DeliveryDatatype { get; set; }

        /// <summary>
        /// Detection limit
        /// </summary>
        public string DetectionLimit { get; set; }

        /// <summary>
        /// Estimated uncertainty
        /// </summary>
        public string EstimationUncertainty { get; set; }

        /// <summary>
        /// Expedition id
        /// </summary>
        public string ExpeditionId { get; set; }

        /// <summary>
        /// Unique id
        /// </summary>
        [BsonId]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Internet access
        /// </summary>
        public string InternetAccess { get; set; }

        /// <summary>
        /// Ice observation code
        /// </summary>
        public string IceObservationCode { get; set; }

        /// <summary>
        /// Method calculation uncertainty
        /// </summary>
        public string MethodCalculationUncertainty { get; set; }

        /// <summary>
        /// Method reference code
        /// </summary>
        public string MethodReferenceCode { get; set; }

        /// <summary>
        /// Monitoring program code
        /// </summary>
        public string MonitoringProgramCode { get; set; }

        /// <summary>
        /// Parameter 
        /// </summary>
        public string Parameter { get; set; }

        /// <summary>
        /// Platform code
        /// </summary>
        public string PlatformCode { get; set; }

        /// <summary>
        /// Position system code
        /// </summary>
        public string PositioningSystemCode { get; set; }

        /// <summary>
        /// Preservation method code
        /// </summary>
        public string PreservationMethodCode { get; set; }

        /// <summary>
        /// Reported station name
        /// </summary>
        public string ReportedStationName { get; set; }

        /// <summary>
        /// Reporting instance name
        /// </summary>
        public string ReportingInstituteNameSv { get; set; }

        /// <summary>
        /// Quality flag
        /// </summary>
        public string QualityFlag { get; set; }

        /// <summary>
        /// Quantification limit
        /// </summary>
        public string QuantificationLimit { get; set; }

        /// <summary>
        /// Comment about sample
        /// </summary>
        public string SampleComment { get; set; }

        /// <summary>
        /// Date of sample
        /// </summary>
        public string SampleDate { get; set; }

        /// <summary>
        /// Sample dept in m
        /// </summary>
        public string SampleDepthM { get; set; }

        /// <summary>
        /// Sampled volume in liters
        /// </summary>
        public string SampledVolumeL { get; set; }

        /// <summary>
        /// End date of sample
        /// </summary>
        public string SampleEndDate { get; set; }

        /// <summary>
        /// End time of sample
        /// </summary>
        public string SampleEndTime { get; set; }

        /// <summary>
        /// Id of sample
        /// </summary>
        public string SampleId { get; set; }

        /// <summary>
        /// Sample latitude dd
        /// </summary>
        public string SampleLatitudeDd { get; set; }

        /// <summary>
        /// Sample latitude dm
        /// </summary>
        public string SampleLatitudeDm { get; set; }

        /// <summary>
        /// Sample longitude dd
        /// </summary>
        public string SampleLongitudeDd { get; set; }

        /// <summary>
        /// Sample longitude Dm
        /// </summary>
        public string SampleLongitudeDm { get; set; }

        /// <summary>
        /// Sample max dept in m
        /// </summary>
        public string SampleMaxDepthM { get; set; }

        /// <summary>
        /// Sample min dept in m
        /// </summary>
        public string SampleMinDepthM { get; set; }

        /// <summary>
        /// Sample orderer name in swedish
        /// </summary>
        public string SampleOrdererNameSv { get; set; }

        /// <summary>
        /// Sample part id
        /// </summary>
        public string SamplePartId { get; set; }

        /// <summary>
        /// Sample project name in swedish
        /// </summary>
        public string SampleProjectNameSv { get; set; }

        /// <summary>
        /// Sample series
        /// </summary>
        public string SampleSeries { get; set; }

        /// <summary>
        /// Sampler type code 
        /// </summary>
        public string SamplerTypeCode { get; set; }

        /// <summary>
        /// Sampler type code phyche
        /// </summary>
        public string SamplerTypeCodePhyche { get; set; }

        /// <summary>
        /// Time of sample
        /// </summary>
        public string SampleTime { get; set; }

        /// <summary>
        /// Sampling laboratory accreditated 
        /// </summary>
        public string SamplingLaboratoryAccreditated { get; set; }

        /// <summary>
        /// Sampling laboratory accreditated  phyche
        /// </summary>
        public string SamplingLaboratoryAccreditatedPhyche { get; set; }

        /// <summary>
        /// Sampling laboratory code phyche
        /// </summary>
        public string SamplingLaboratoryCodePhyche { get; set; }

        /// <summary>
        /// Sampling laboratory names in swedish
        /// </summary>
        public string SamplingLaboratoryNamesv { get; set; }

        /// <summary>
        /// Sampling method comment phyche
        /// </summary>
        public string SamplingMethodCommentPhyche { get; set; }

        /// <summary>
        /// Sampling method reference code phyche
        /// </summary>
        public string SamplingMethodReferenceCodePhyche { get; set; }

        /// <summary>
        /// Taxon scientific name
        /// </summary>
        public string ScientificName { get; set; }

        /// <summary>
        /// Secchi dept 
        /// </summary>
        public string SecchiDepthM { get; set; }

        /// <summary>
        /// Secch dept quality flag
        /// </summary>
        public string SecchiDepthQualityFlag { get; set; }

        /// <summary>
        /// Shark sample id
        /// </summary>
        public string SharkSampleId { get; set; }

        /// <summary>
        /// Shark sample id
        /// </summary>
        public string Sharksampleidmd5 { get; set; }

        /// <summary>
        /// Station name
        /// </summary>
        public string StationName { get; set; }

        /// <summary>
        /// Station viss eu id
        /// </summary>
        public string StationVissEuId { get; set; }

        /// <summary>
        /// Unit of value
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Value property
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Comment of variable
        /// </summary>
        public string VariableComment { get; set; }

        /// <summary>
        /// Comment of visit
        /// </summary>
        public string VisitComment { get; set; }

        /// <summary>
        /// Date of visit
        /// </summary>
        public string VisitDate { get; set; }

        /// <summary>
        /// Id of visit
        /// </summary>
        public string VisitId { get; set; }

        /// <summary>
        /// Visit year
        /// </summary>
        public string VisitYear { get; set; }

        /// <summary>
        /// Water dept in m
        /// </summary>
        public string WaterDepthM { get; set; }

        /// <summary>
        /// Waterland station type code
        /// </summary>
        public string WaterlandStationTypeCode { get; set; }

        /// <summary>
        /// Wave observation code
        /// </summary>
        public string WaveObservationCode { get; set; }

        /// <summary>
        /// Weather observation code
        /// </summary>
        public string WeatherObservationCode { get; set; }

        /// <summary>
        /// Wind direction code
        /// </summary>
        public string WindDirectionCode { get; set; }

        /// <summary>
        /// Wind speed in m/s   
        /// </summary>
        public string WindSpeedMs { get; set; }
    }
}