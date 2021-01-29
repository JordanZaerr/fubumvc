using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using FubuMVC.Core.ServiceBus.Configuration;

namespace FubuMVC.Core.ServiceBus.Runtime
{
    public interface ITransport : IDisposable
    {
        void OpenChannels(ChannelGraph graph);
        string Protocol { get; }

        /// <summary>
        /// This is mostly for the cases where we have
        /// to register new channels at runtime through
        /// dynamic subscriptions or reply channels
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        IChannel BuildDestinationChannel(Uri destination, X509Certificate transportCertificate = null);

        IEnumerable<EnvelopeToken> ReplayDelayed(DateTime currentTime);

        void ClearAll();
    }
}
