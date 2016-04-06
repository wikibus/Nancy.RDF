﻿using System.IO;
using System.Reflection;
using JsonLD.Entities;
using Nancy.ModelBinding;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;

namespace Nancy.Rdf.ModelBinding
{
    /// <summary>
    /// Converts body, first converting it to NQuads
    /// </summary>
    public abstract class NonJsonLdRdfBodyDeserializer : RdfBodyDeserializer
    {
        private static readonly MethodInfo DeserializeNquadsMethod = Info.OfMethod(
            "JsonLd.Entities",
            "JsonLD.Entities.IEntitySerializer",
            "Deserialize",
            "String");

        private static readonly IRdfWriter RdfWriter = new NTriplesWriter(NTriplesSyntax.Rdf11);
        private readonly IRdfReader _reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonJsonLdRdfBodyDeserializer"/> class.
        /// </summary>
        protected NonJsonLdRdfBodyDeserializer(
            RdfSerialization serialization,
            IEntitySerializer serializer,
            IRdfReader reader) : base(serialization, serializer)
        {
            _reader = reader;
        }

        /// <summary>
        /// Deserialize the request body to a model
        /// </summary>
        public override object Deserialize(string contentType, Stream body, BindingContext context)
        {
            var deserialize = DeserializeNquadsMethod.MakeGenericMethod(context.DestinationType);

            return deserialize.Invoke(_serializer, new object[] { GetNquads(body) });
        }

        /// <summary>
        /// Converts body to N-Triples
        /// </summary>
        protected virtual string GetNquads(Stream body)
        {
            // todo: implement actual parsers for json-ld.net so that it's not necessary to parse and write to ntriples
            IGraph g = new Graph();

            using (var streamReader = new StreamReader(body))
            {
                _reader.Load(g, streamReader);
            }

            using (var stringWriter = new StringWriter())
            {
                RdfWriter.Save(g, stringWriter);
                return stringWriter.ToString();
            }
        }
    }
}
