﻿using System;
using System.Collections.Generic;
using System.IO;
using FakeItEasy;
using Nancy.Rdf.Responses;
using Nancy.Responses.Negotiation;
using NUnit.Framework;

namespace Nancy.Rdf.Tests
{
    [TestFixture]
    public class RdfResponseProcessorTests
    {
        private const string FallbackSerializationKey = "__nrfs";

        private static readonly NancyContext NancyContext;

        static RdfResponseProcessorTests()
        {
            var path = new Url("http://example.com/api/test")
            {
                BasePath = "api"
            };

            NancyContext = new NancyContext
            {
                Request = new Request("GET", path)
            };
        }

        [Test]
        public void Should_not_allow_processing_when_no_compatible_serializer_is_available()
        {
            // given
            var serializers = new ISerializer[0];
            var processor = new RdfResponseProcessorTestable(serializers);

            // when
            var match = processor.CanProcess(new MediaRange(RdfSerialization.RdfXml.MediaType), new object(), new NancyContext());

            // then
            Assert.That(match.ModelResult, Is.EqualTo(MatchResult.DontCare));
            Assert.That(match.RequestedContentTypeResult, Is.EqualTo(MatchResult.NoMatch));
        }

        [Test]
        public void Should_match_wildcard_when_set_up()
        {
            // given
            var serializer = A.Fake<RdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var nancyContext = new NancyContext();
            nancyContext.Items.Add(FallbackSerializationKey, RdfSerialization.RdfXml);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            // when
            var match = processor.CanProcess(new MediaRange("*/*"), new object(), nancyContext);

            // then
            Assert.That(match.ModelResult, Is.EqualTo(MatchResult.DontCare));
            Assert.That(match.RequestedContentTypeResult, Is.EqualTo(MatchResult.NonExactMatch));
        }

        [Test]
        public void Should_not_match_wildcard_when_another_fallback_set_up()
        {
            // given
            var serializer = A.Fake<RdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var nancyContext = new NancyContext();
            nancyContext.Items.Add(FallbackSerializationKey, RdfSerialization.Turtle);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            // when
            var match = processor.CanProcess(new MediaRange("*/*"), new object(), nancyContext);

            // then
            Assert.That(match.ModelResult, Is.EqualTo(MatchResult.DontCare));
            Assert.That(match.RequestedContentTypeResult, Is.EqualTo(MatchResult.NoMatch));
        }

        [Test]
        public void Should_not_match_wildcard_when_not_set_up()
        {
            // given
            var serializer = A.Fake<RdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            // when
            var match = processor.CanProcess(new MediaRange("*/*"), new object(), new NancyContext());

            // then
            Assert.That(match.ModelResult, Is.EqualTo(MatchResult.DontCare));
            Assert.That(match.RequestedContentTypeResult, Is.EqualTo(MatchResult.NoMatch));
        }

        [Test]
        public void Should_pass_SiteBase_from_context_to_serializtion()
        {
            // given
            var serializer = A.Fake<IRdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            // when
            var response = processor.Process(new MediaRange("application/rdf+xml"), new object(), NancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                A<MediaRange>.That.Matches(mr => mr == RdfSerialization.RdfXml.MediaType),
                A<WrappedModel>.That.Matches(wm => wm.BaseUrl == new Uri("http://example.com/")),
                A<MemoryStream>._)).MustHaveHappened();
        }

        [Test]
        public void Should_use_SSL_if_x_Forwarded_Proto_header_present()
        {
            // given
            var serializer = A.Fake<IRdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            var path = new Url("http://example.com/api/test")
            {
                BasePath = "api"
            };

            var nancyContext = new NancyContext
            {
                Request = new Request("GET", path, headers: new Dictionary<string, IEnumerable<string>>
                {
                    {"X-Forwarded-Proto", new []{"https"}}
                })
            };

            // when
            var response = processor.Process(new MediaRange("application/rdf+xml"), new object(), nancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                A<MediaRange>.That.Matches(mr => mr == RdfSerialization.RdfXml.MediaType),
                A<WrappedModel>.That.Matches(wm => wm.BaseUrl.ToString() == "https://example.com/"),
                A<MemoryStream>._)).MustHaveHappened();
        }

        [Test]
        public void Should_not_contain_unnecessary_443_port_in_base_url()
        {
            // given
            var serializer = A.Fake<IRdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            var path = new Url("https://example.com/api/test")
            {
                BasePath = "api"
            };

            var nancyContext = new NancyContext
            {
                Request = new Request("GET", path)
            };

            // when
            var response = processor.Process(new MediaRange("application/rdf+xml"), new object(), nancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                A<MediaRange>.That.Matches(mr => mr == RdfSerialization.RdfXml.MediaType),
                A<WrappedModel>.That.Matches(wm => wm.BaseUrl.ToString() == "https://example.com/"),
                A<MemoryStream>._)).MustHaveHappened();
        }

        [Test]
        public void Should_not_preserve_custom_port_in_base_url()
        {
            // given
            var serializer = A.Fake<IRdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            var path = new Url("http://example.com:2345/api/test")
            {
                BasePath = "api"
            };

            var nancyContext = new NancyContext
            {
                Request = new Request("GET", path)
            };

            // when
            var response = processor.Process(new MediaRange("application/rdf+xml"), new object(), nancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                A<MediaRange>.That.Matches(mr => mr == RdfSerialization.RdfXml.MediaType),
                A<WrappedModel>.That.Matches(wm => wm.BaseUrl.ToString() == "http://example.com:2345/"),
                A<MemoryStream>._)).MustHaveHappened();
        }

        [Test]
        public void Should_pass_actual_requested()
        {
            // given
            var contentType = new MediaRange("application/rdf+xml;profile=testprofile");
            var serializer = A.Fake<IRdfSerializer>();
            A.CallTo(() => serializer.CanSerialize(A<MediaRange>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });

            // when
            var response = processor.Process(new MediaRange(contentType), new object(), NancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                A<MediaRange>.That.Matches(mr => mr.Equals(contentType)),
                A<WrappedModel>._,
                A<MemoryStream>._)).MustHaveHappened();
        }

        [Test]
        public void CanProcess_Should_not_match_model_is_null()
        {
            // given
            var processor = new RdfResponseProcessorTestable(new ISerializer[0]);

            // when
            var match = processor.CanProcess(new MediaRange(RdfSerialization.RdfXml.MediaType), null, new NancyContext());

            // then
            Assert.That(match.ModelResult, Is.EqualTo(MatchResult.NoMatch));
        }

        private class RdfResponseProcessorTestable : RdfResponseProcessor
        {
            public RdfResponseProcessorTestable(IEnumerable<ISerializer> serializers)
                : base(RdfSerialization.RdfXml, serializers)
            {
            }
        }
    }
}
