﻿using System;
using System.Collections.Generic;
using Nest;

namespace SOS.Lib.Models.Processed.Observation
{
    /// <summary>
    ///     Event information about a species observation.
    /// </summary>
    public class Event
    {
        /// <summary>
        ///    DiscoveryMethod from Artportalen.
        /// </summary>
        [Object]
        public VocabularyValue DiscoveryMethod { get; set; }

        /// <summary>
        ///     End date/time of the event (UTC).
        /// </summary>
        [Date]
        public DateTime? EndDate { get; set; }

        /// <summary>
        ///     An identifier for the set of information associated with an Event (something that occurs at a place and time). 
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        ///     Comments or notes about the Event.
        /// </summary>
        public string EventRemarks { get; set; }

        /// <summary>
        ///     One of a) an indicator of the existence of, b) a
        ///     reference to (publication, URI), or c) the text of
        ///     notes taken in the field about the Event.
        /// </summary>
        public string FieldNotes { get; set; }

        /// <summary>
        ///     An identifier given to the event in the field. Often
        ///     serves as a link between field notes and the Event.
        /// </summary>
        public string FieldNumber { get; set; }

        /// <summary>
        ///     A category or description of the habitat in which the Event occurred.
        /// </summary>
        public string Habitat { get; set; }

        /// <summary>
        /// An identifier for the broader Event that groups this and potentially other Events.
        /// </summary>
        /// <example>
        ///     A1 (parentEventID to identify the main Whittaker Plot in nested samples, each with its own eventID - A1:1, A1:2).
        /// </example>
        public string ParentEventId { get; set; }

        /// <summary>
        ///     The amount of effort expended during an Event.
        /// </summary>
        public string SamplingEffort { get; set; }

        /// <summary>
        ///     The name of, reference to, or description of the
        ///     method or protocol used during an Event.
        /// </summary>
        public string SamplingProtocol { get; set; }

        /// <summary>
        ///     The unit of measurement of the size (time duration, length, area, or volume) of a sample in a sampling event.
        ///     A sampleSizeUnit must have a corresponding sampleSizeValue, e.g., 5 for sampleSizeValue with metre for
        ///     sampleSizeUnit.
        /// </summary>
        /// <example>
        ///     minute, hour, day, metre, square metre, cubic metre.
        /// </example>
        public string SampleSizeUnit { get; set; }

        /// <summary>
        ///     A numeric value for a measurement of the size (time duration, length, area, or volume) of a sample in a sampling
        ///     event.
        /// </summary>
        /// <example>
        ///     5 for sampleSizeValue with metre for sampleSizeUnit.
        /// </example>
        public string SampleSizeValue { get; set; }

        /// <summary>
        ///     Start date/time of the event (UTC).
        /// </summary>
        [Date]
        public DateTime? StartDate { get; set; }

        /// <summary>
        ///     The verbatim original representation of the date and time information for an Event.
        ///     Examples: spring 1910, Marzo 2002, 1999-03-XX, 17IV1934.
        /// </summary>
        public string VerbatimEventDate { get; set; }

        /// <summary>
        ///     Multimedia associated with the event.
        /// </summary>
        [Nested]
        public ICollection<Multimedia> Media { get; set; }

        /// <summary>
        ///     Measurement or facts associated with the event.
        /// </summary>
        [Nested]
        public ICollection<ExtendedMeasurementOrFact> MeasurementOrFacts { get; set; }
    }
}