using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reactive.Concurrency;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FubuCore.Util;
using LightningDB;
using FubuCore.Logging;
using LightningQueues;
using LightningQueues.Storage;
using LightningQueues.Storage.LMDB;

namespace FubuMVC.LightningQueues
{
    public class ManagerCacheKey
    {
        public ManagerCacheKey(int port, string certHash)
        {
            Port = port;
            CertHash = certHash;
        }

        protected bool Equals(ManagerCacheKey other)
        {
            return Port == other.Port && CertHash == other.CertHash;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ManagerCacheKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Port * 397) ^ (CertHash != null ? CertHash.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ManagerCacheKey left, ManagerCacheKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ManagerCacheKey left, ManagerCacheKey right)
        {
            return !Equals(left, right);
        }

        public int Port { get; set; }
        public string CertHash { get; set; }

    }

    public class PersistentQueues : IPersistentQueues
    {
        private readonly ILogger _logger;
        public string QueuePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fubutransportationqueues");

        private readonly Cache<ManagerCacheKey, Queue> _queueManagers;

        public PersistentQueues(ILogger logger)
        {
            _logger = logger;
            _queueManagers = new Cache<ManagerCacheKey, Queue>();
        }

        private Queue GetQueue(int port, bool persist, bool incoming, int mapSize = 1024*1024*100, int maxDatabases = 5, X509Certificate certificate = null)
        {
            if (!incoming)
            {
                //Shouldn't create one here because it shouldn't be listening
                var matchingKey = _queueManagers.GetAllKeys().First(x => x.CertHash == certificate?.GetCertHashString());
                return _queueManagers[matchingKey];
            }
            if (_queueManagers.Has(new ManagerCacheKey(port, certificate?.GetCertHashString())))
            {
                return _queueManagers[new ManagerCacheKey(port, certificate?.GetCertHashString())];
            }
            return CreateQueue(port, persist, mapSize, maxDatabases, certificate);
        }

        private Queue CreateQueue(int port, bool persist, int mapSize = 1024*1024*100, int maxDatabases = 5, X509Certificate certificate = null)
        {
            var queueConfiguration = new QueueConfiguration()
                .SecureQueue(certificate, _logger)
                .ReceiveMessagesAt(new IPEndPoint(IPAddress.Any, port))
                .ScheduleQueueWith(TaskPoolScheduler.Default)
                .LogWith(new FubuLoggingAdapter(_logger));

            if (persist)
            {
                queueConfiguration.StoreWithLmdb(QueuePath + "." + port, new EnvironmentConfiguration { MaxDatabases = maxDatabases, MapSize = mapSize });
            }
            else
            {
                queueConfiguration.UseNoStorage();
            }
            var queue = queueConfiguration.BuildQueue();
            _queueManagers.Fill(new ManagerCacheKey(port, certificate?.GetCertHashString()), queue);
            return queue;
        }


        public void Dispose()
        {
            _queueManagers.Each(x => x.Dispose());
        }

        public IEnumerable<Queue> AllQueueManagers => _queueManagers.GetAll();

        public void ClearAll()
        {
            _queueManagers.Each(x => x.Store.ClearAllStorage());
        }

        public Queue PersistentManagerFor(int port, bool incoming, int mapSize = 1024*1024*100, int maxDatabases = 5, X509Certificate certificate = null)
        {
            return GetQueue(port, true, incoming, mapSize, maxDatabases, certificate);
        }

        public Queue NonPersistentManagerFor(int port, bool incoming, X509Certificate certificate = null)
        {
            return GetQueue(port, false, incoming, certificate: certificate);
        }

        public Queue ManagerForReply(X509Certificate certificate = null)
        {
            //return _queueManagers[new ManagerCacheKey(port, certificate?.GetCertHashString())];
            var matchingKey = _queueManagers.GetAllKeys().First(x => x.CertHash == certificate?.GetCertHashString());
            return _queueManagers[matchingKey];
        }

        public void Start(IEnumerable<ITransportUri> uriList)
        {
            uriList.GroupBy(x => x.Port).Each(group =>
            {
                try
                {
                    string[] queueNames = group.Select(x => x.QueueName).ToArray();
                    List<X509Certificate> certificates = group.Select(x => x.Certificate).ToList();

                    if (certificates.GroupBy(x => x?.GetCertHashString()).Count() > 1)
                    {
                        throw new InvalidOperationException("All queues hosted on the same port must use the same certificate");
                    }

                    var queueManager = GetQueue(@group.Key, true, true, certificate: certificates.FirstOrDefault());
                    queueNames.Each(x => queueManager.CreateQueue(x));
                    queueManager.CreateQueue(LightningQueuesTransport.ErrorQueueName);
                    queueManager.Start();
                }
                catch (Exception e)
                {
                    throw new LightningQueueTransportException(new IPEndPoint(IPAddress.Any, group.Key), e);
                }
            });
        }
    }

    internal static class PersistentQueuesExtensions
    {
        public static QueueConfiguration SecureQueue(this QueueConfiguration queueConfiguration, X509Certificate certificate, ILogger logger)
        {
            if (certificate == null) return queueConfiguration;

            return queueConfiguration.SecureTransportWith(async (_, receiving) =>
            {
                var sslStream = new SslStream(receiving, false);
                try
                {
                    await sslStream.AuthenticateAsServerAsync(certificate, false,
                        checkCertificateRevocation: false, enabledSslProtocols: SslProtocols.Tls12);
                }
                catch (Exception ex)
                {
                    logger.Error("Transport security error receiving message", ex);
                    throw;
                }

                return sslStream;
            }, async (uri, sending) =>
            {
                bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain,
                    SslPolicyErrors sslPolicyErrors)
                {
                    return true; //we only care that the transport is encrypted
                }

                var sslStream = new SslStream(sending, false, ValidateServerCertificate, null);

                try
                {
                    await sslStream.AuthenticateAsClientAsync(uri.Host);
                }
                catch (Exception ex)
                {
                    logger.Error("Transport security error sending message", ex);
                    throw;
                }

                return sslStream;
            });
        }
    }

    [Serializable]
    public class LightningQueueTransportException : Exception
    {
        public LightningQueueTransportException(IPEndPoint endpoint, Exception innerException) : base("Error trying to initialize LightningQueues queue manager at " + endpoint, innerException)
        {
        }

        protected LightningQueueTransportException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
