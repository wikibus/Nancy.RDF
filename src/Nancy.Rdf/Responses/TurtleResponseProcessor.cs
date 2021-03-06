﻿using System.Collections.Generic;

namespace Nancy.Rdf.Responses
{
    /// <summary>
    /// Response processor for Turtle
    /// </summary>
    public class TurtleResponseProcessor : RdfResponseProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TurtleResponseProcessor"/> class.
        /// </summary>
        /// <param name="serializers">The serializers.</param>
        public TurtleResponseProcessor(IEnumerable<ISerializer> serializers)
            : base(RdfSerialization.Turtle, serializers)
        {
        }
    }
}
