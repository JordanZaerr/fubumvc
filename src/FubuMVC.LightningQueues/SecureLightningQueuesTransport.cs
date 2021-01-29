using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FubuMVC.Core.ServiceBus.Configuration;
using FubuMVC.Core.ServiceBus.Runtime;

namespace FubuMVC.LightningQueues
{
    public class SecureLightningQueuesTransport : TransportBase, ITransport
    {
        public static readonly string ErrorQueueName = "errors";

        private readonly IPersistentQueues _queues;
        private readonly LightningQueueSettings _settings;

        public SecureLightningQueuesTransport(IPersistentQueues queues, LightningQueueSettings settings)
        {
            _queues = queues;
            _settings = settings;
        }

        public void Dispose()
        {
            // IPersistentQueues is disposable
        }

        public override string Protocol => SecureLightningUri.Protocol;

        public override bool Disabled(IEnumerable<ChannelNode> nodes)
        {
            if (_settings.Disabled) return true;

            if (!nodes.Any() && _settings.DisableIfNoChannels) return true;

            return false;
        }

        public IChannel BuildDestinationChannel(Uri destination, X509Certificate certificate)
        {
            //This is probably going to break volatile subscribers
            var lqUri = new SecureLightningUri(destination, certificate);
            return new LightningQueuesReplyChannel(destination, _queues.ManagerForReply(certificate), lqUri.QueueName);
        }

        public IEnumerable<EnvelopeToken> ReplayDelayed(DateTime currentTime)
        {
            return Enumerable.Empty<EnvelopeToken>();
        }

        public void ClearAll()
        {
            _queues.ClearAll();
        }

        protected override IChannel buildChannel(ChannelNode channelNode)
        {
            var uri = new SecureLightningUri(channelNode.Uri, channelNode.TransportCertificate);
            return channelNode.Mode == ChannelMode.DeliveryGuaranteed
                ? LightningQueuesChannel.BuildPersistentChannel(uri, _queues, _settings.MapSize, _settings.MaxDatabases, channelNode.Incoming, channelNode.TransportCertificate)
                : LightningQueuesChannel.BuildNoPersistenceChannel(uri, _queues, channelNode.Incoming, channelNode.TransportCertificate);
        }

        protected override void seedQueues(IEnumerable<ChannelNode> channels)
        {
            var groups = channels.Where(x => x.Incoming).GroupBy(x => x.Uri.Port).Where(x => x.Count() > 1);
            foreach (var group in groups)
            {
                if (group.Select(x => x.Mode).Distinct().Count() > 1)
                {
                    throw new InvalidOperationException($"The LightningQueues listener for port {group.Key} has mixed channel modes. Either make the modes be the same or use a different port number");
                }
            }


            _queues.Start(channels.Where(x => x.Incoming).Select(x => new SecureLightningUri(x.Uri, x.TransportCertificate)));
        }

        protected override Uri getReplyUri(ChannelGraph graph)
        {
            var channelNode = graph.FirstOrDefault(x => x.Protocol() == SecureLightningUri.Protocol && x.Incoming && x.Mode == ChannelMode.DeliveryGuaranteed)
                              ?? graph.FirstOrDefault(x => x.Protocol() == SecureLightningUri.Protocol && x.Incoming && x.Mode == ChannelMode.DeliveryFastWithoutGuarantee);
            if (channelNode == null)
                throw new InvalidOperationException("You must have at least one incoming Lightning Queue channel for accepting replies");

            return channelNode.Channel.Address.ToLocalUri();
        }
    }
}
