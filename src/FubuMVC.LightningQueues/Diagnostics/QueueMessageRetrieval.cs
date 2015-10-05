using System;
using System.Collections.Generic;
using System.Linq;
using LightningQueues;

namespace FubuMVC.LightningQueues.Diagnostics
{
    public class QueueMessageRetrieval : IQueueMessageRetrieval
    {
        private readonly IPersistentQueues _queues;

        private static readonly IEnumerable<QueueMessagesRetrievalStrategy> MessageRetrievalStrategies =
            new List<QueueMessagesRetrievalStrategy>
            {
                new QueueMessagesRetrievalStrategy
                {
                    CanHandle = req => req.QueueName == "outgoing",
                    //Execute = (req, queue) => queue.GetMessagesCurrentlySending(),
                    //ExecuteForSingleMessage = (req, queue) => queue.GetMessageCurrentlySendingById(req.MessageId)
                },
                new QueueMessagesRetrievalStrategy
                {
                    CanHandle = req => req.QueueName == "outgoing_history",
                    //Execute = (req, queue) => queue.GetAllSentMessages(),
                    //ExecuteForSingleMessage = (req, queue) => queue.GetSentMessageById(req.MessageId)
                },
                new QueueMessagesRetrievalStrategy
                {
                    CanHandle = req => req.QueueName.EndsWith("_history"),
                    //Execute = (req, queue) => queue.GetAllProcessedMessages(req.QueueName.WithoutHistorySuffix()),
                    //ExecuteForSingleMessage = (req, queue) => queue.GetProcessedMessageById(req.QueueName.WithoutHistorySuffix(), req.MessageId)
                },
                new QueueMessagesRetrievalStrategy
                {
                    CanHandle = _ => true,
                    //Execute = (req, queue) => queue.GetAllMessages(req.QueueName, null),
                    //ExecuteForSingleMessage = (req, queue) => queue.PeekById(req.QueueName, req.MessageId)
                }
            };

        public QueueMessageRetrieval(IPersistentQueues queues)
        {
            _queues = queues;
        }

        public IEnumerable<Message> GetAllMessagesInQueue(QueueMessageRetrievalRequest request)
        {
            return MessageRetrievalStrategies
                .First(x => x.CanHandle(request))
                .Execute(request, GetQueueManager(request));
        }

        public Message GetSingleMessageInQueue(QueueMessageRetrievalRequest request)
        {
            return MessageRetrievalStrategies
                .First(x => x.CanHandle(request))
                .ExecuteForSingleMessage(request, GetQueueManager(request));
        }

        private Queue GetQueueManager(QueueMessageRetrievalRequest request)
        {
            return _queues.AllQueueManagers.Single(x => x.Endpoint.Port == request.Port);
        }

        private class QueueMessagesRetrievalStrategy
        {
            public Func<QueueMessageRetrievalRequest, bool> CanHandle { get; set; }
            public Func<QueueMessageRetrievalRequest, Queue, IEnumerable<Message>> Execute { get; set; }
            public Func<QueueMessageRetrievalRequest, Queue, Message> ExecuteForSingleMessage { get; set; }
        }
    }

    public interface IQueueMessageRetrieval
    {
        IEnumerable<Message> GetAllMessagesInQueue(QueueMessageRetrievalRequest request);
        Message GetSingleMessageInQueue(QueueMessageRetrievalRequest request);
    }

    public class QueueMessageRetrievalRequest
    {
        public int Port { get; set; }
        public string QueueName { get; set; }
        public MessageId MessageId { get; set; }
    }

    public static class QueueNameExtensions
    {
        public static string WithoutHistorySuffix(this string queueName)
        {
            return queueName.Replace("_history", string.Empty);
        }
    }
}
