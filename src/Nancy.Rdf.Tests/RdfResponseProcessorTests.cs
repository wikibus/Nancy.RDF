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
            A.CallTo(() => serializer.CanSerialize(A<string>.Ignored)).Returns(true);
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
            A.CallTo(() => serializer.CanSerialize(A<string>.Ignored)).Returns(true);
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
            A.CallTo(() => serializer.CanSerialize(A<string>.Ignored)).Returns(true);
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
            A.CallTo(() => serializer.CanSerialize(A<string>.Ignored)).Returns(true);
            var processor = new RdfResponseProcessorTestable(new[] { serializer });
            var nancyContext = new NancyContext
            {
                Request = new Request(
                    "GET",
                    new Url("http://example.com/api/test")
                    {
                        BasePath = "api"
                    })
            };

            // when
            var response = processor.Process(new MediaRange("application/rdf+xml"), new object(), nancyContext);
            response.Contents(new MemoryStream());

            // then
            A.CallTo(() => serializer.Serialize(
                RdfSerialization.RdfXml.MediaType,
                A<WrappedModel>.That.Matches(wm => wm.BaseUrl == new Uri(nancyContext.Request.Url.SiteBase)),
                A<MemoryStream>._)).MustHaveHappened();
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
