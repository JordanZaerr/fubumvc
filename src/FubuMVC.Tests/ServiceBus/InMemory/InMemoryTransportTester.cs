﻿using System;
using FubuMVC.Core.ServiceBus;
using FubuMVC.Core.ServiceBus.Configuration;
using FubuMVC.Core.ServiceBus.InMemory;
using FubuMVC.Core.StructureMap;
using NUnit.Framework;
using FubuTestingSupport;

namespace FubuTransportation.Testing.InMemory
{
    [TestFixture]
    public class InMemoryTransportTester
    {
        [Test]
        public void to_in_memory()
        {
            var settings = InMemoryTransport.ToInMemory<NodeSettings>();

            settings.Inbound.ShouldBe(new Uri("memory://node/inbound"));
            settings.Outbound.ShouldBe(new Uri("memory://node/outbound"));
        }

        [Test]
        public void to_in_memory_with_default_settings()
        {
            FubuTransport.SetupForInMemoryTesting<DefaultSettings>();
            using (var runtime = FubuTransport.For<DefaultRegistry>().Bootstrap())
            {
                var settings = InMemoryTransport.ToInMemory<NodeSettings>();
                settings.Inbound.ShouldBe(new Uri("memory://default/inbound"));
                settings.Outbound.ShouldBe(new Uri("memory://node/outbound"));
            }
        }

        [Test]
        public void default_reply_uri()
        {
            using (var runtime = FubuTransport.For(x => {
                x.EnableInMemoryTransport();
            }).Bootstrap())
            {
                runtime.Factory.Get<ChannelGraph>().ReplyChannelFor(InMemoryChannel.Protocol)
                    .ShouldBe("memory://localhost/fubu/replies".ToUri());
            }
        }

        [Test]
        public void override_the_reply_uri()
        {
            using (var runtime = FubuTransport.For(x =>
            {
                x.EnableInMemoryTransport("memory://special".ToUri());
            }).Bootstrap())
            {
                runtime.Factory.Get<ChannelGraph>().ReplyChannelFor(InMemoryChannel.Protocol)
                    .ShouldBe("memory://special".ToUri());
            }
        }

        private class DefaultSettings
        {
            public Uri Inbound { get; set; }
        }

        private class DefaultRegistry : FubuTransportRegistry<DefaultSettings>
        {
            public DefaultRegistry()
            {
                Channel(x => x.Inbound).ReadIncoming();
            }
        }
    }

    public class NodeSettings
    {
        public Uri Inbound { get; set; }
        public Uri Outbound { get; set; }

        public string Something { get; set; }
    }
}