﻿using System.Collections.Generic;

namespace Nancy.RDF.Responses
{
    /// <summary>
    /// Response processor for Turtle
    /// </summary>
    public class Notation3ResponseProcessor : RdfResponseProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notation3ResponseProcessor"/> class.
        /// </summary>
        /// <param name="serializers">The serializers.</param>
        /// <param name="options">Response processing options</param>
        public Notation3ResponseProcessor(IEnumerable<ISerializer> serializers, RdfResponseOptions options)
            : base(RdfSerialization.Notation3, serializers, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Notation3ResponseProcessor"/> class.
        /// </summary>
        /// <param name="serializers">The serializers.</param>
        public Notation3ResponseProcessor(IEnumerable<ISerializer> serializers)
            : base(RdfSerialization.Notation3, serializers, new RdfResponseOptions())
        {
        }
    }
}
