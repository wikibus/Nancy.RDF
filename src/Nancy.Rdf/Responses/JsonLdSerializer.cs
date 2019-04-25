﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.Entities;
using Nancy.IO;
using Nancy.Rdf.Contexts;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json.Linq;

namespace Nancy.Rdf.Responses
{
    /// <summary>
    /// Serializer of JSON-LD
    /// </summary>
    public class JsonLdSerializer : IRdfSerializer
    {
        private static readonly RdfSerialization JsonLdSerialization = RdfSerialization.JsonLd;
        private readonly IEntitySerializer serializer;
        private readonly IContextPathMapper contextPathMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonLdSerializer"/> class.
        /// </summary>
        public JsonLdSerializer(IEntitySerializer serializer, IContextPathMapper contextPathMapper)
        {
            this.serializer = serializer;
            this.contextPathMapper = contextPathMapper;
        }

        /// <summary>
        /// Gets the list of extensions that the serializer can handle.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of extensions if any are available, otherwise an empty enumerable.
        /// </value>
        public IEnumerable<string> Extensions
        {
            get { yield return JsonLdSerialization.Extension; }
        }

        /// <summary>
        /// Whether the serializer can serialize the content type
        /// </summary>
        /// <param name="mediaRange">Content type to serialize</param>
        /// <returns>
        /// True if supported, false otherwise
        /// </returns>
        public bool CanSerialize(MediaRange mediaRange)
        {
            return JsonLdSerialization.MediaType.Equals(mediaRange, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Serializes the specified content type.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="mediaRange">Type of the content.</param>
        /// <param name="model">The model.</param>
        /// <param name="outputStream">The output stream.</param>
        public void Serialize<TModel>(MediaRange mediaRange, TModel model, Stream outputStream)
        {
            WrappedModel? wrappedModel = model as WrappedModel?;
            var actualModel = wrappedModel == null ? model : wrappedModel.Value.Model;

            using (var writer = new StreamWriter(new UnclosableStreamWrapper(outputStream)))
            {
                JToken serialized = this.serializer.Serialize(actualModel);

                if (wrappedModel.HasValue)
                {
                    serialized.AddBaseToContext(wrappedModel.Value.BaseUrl);
                }

                if (mediaRange.Parameters["profile"]?.Trim('"') == JsonLdProfiles.Expanded)
                {
                    serialized = JsonLdProcessor.Expand(serialized);
                }
                else
                {
                    if (serialized[JsonLdKeywords.Context] != null)
                    {
                        var contextMap = this.contextPathMapper.Contexts.FirstOrDefault(map => map.ModelType == actualModel.GetType());

                        if (contextMap != default(ContextPathMap) && wrappedModel != null)
                        {
                            var newContext = wrappedModel.Value.BaseUrl
                                .Append(this.contextPathMapper.BasePath)
                                .Append(contextMap.Path);
                            serialized[JsonLdKeywords.Context] = newContext;
                        }
                    }
                }

                Debug.WriteLine("Serialized model: {0}", new object[] { serialized });

                writer.Write(serialized);
            }
        }
    }
}
