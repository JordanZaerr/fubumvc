using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using LightningQueues;

namespace FubuMVC.LightningQueues
{
    public interface IPersistentQueues : IDisposable
    {
        IEnumerable<Queue> AllQueueManagers { get; }
        void ClearAll();
        Queue PersistentManagerFor(int port, bool incoming, int mapSize = 1024*1024*100, int maxDatabases = 5, X509Certificate certificate = null);
        Queue NonPersistentManagerFor(int port, bool incoming, X509Certificate certificate = null);
        Queue ManagerForReply(X509Certificate certificate = null);
        void Start(IEnumerable<ITransportUri> uriList);
    }
}
