using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace HealthyPlant.Grpc.Jobs
{
    public interface IAppFirebaseMessaging
    {
        Task<string> SendAsync(Message message);
        Task<string> SendAsync(Message message, CancellationToken cancellationToken);
        Task<string> SendAsync(Message message, bool dryRun);
        Task<string> SendAsync(Message message, bool dryRun, CancellationToken cancellationToken);
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages);
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun);
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun, CancellationToken cancellationToken);
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message);
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, CancellationToken cancellationToken);
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun);
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun, CancellationToken cancellationToken);
        Task<TopicManagementResponse> SubscribeToTopicAsync(IReadOnlyList<string> registrationTokens, string topic);
        Task<TopicManagementResponse> UnsubscribeFromTopicAsync(IReadOnlyList<string> registrationTokens, string topic);
    }

    public class AppFirebaseMessaging : IAppFirebaseMessaging
    {
        private readonly FirebaseMessaging _firebaseMessaging;

        public AppFirebaseMessaging(IConfiguration configuration)
        {
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(configuration["GOOGLE_APPLICATION_CREDENTIALS"])
            });

            _firebaseMessaging = FirebaseMessaging.GetMessaging(app);
        }

        public Task<string> SendAsync(Message message)
        {
            return _firebaseMessaging.SendAsync(message);
        }

        public Task<string> SendAsync(Message message, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendAsync(message, cancellationToken);
        }

        public Task<string> SendAsync(Message message, bool dryRun)
        {
            return _firebaseMessaging.SendAsync(message, dryRun);
        }

        public Task<string> SendAsync(Message message, bool dryRun, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendAsync(message, dryRun, cancellationToken);
        }

        public Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages)
        {
            return _firebaseMessaging.SendAllAsync(messages);
        }

        public Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendAllAsync(messages, cancellationToken);
        }

        public Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun)
        {
            return _firebaseMessaging.SendAllAsync(messages, dryRun);
        }

        public Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendAllAsync(messages, dryRun, cancellationToken);
        }

        public Task<BatchResponse> SendMulticastAsync(MulticastMessage message)
        {
            return _firebaseMessaging.SendMulticastAsync(message);
        }

        public Task<BatchResponse> SendMulticastAsync(MulticastMessage message, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendMulticastAsync(message, cancellationToken);
        }

        public Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun)
        {
            return _firebaseMessaging.SendMulticastAsync(message, dryRun);
        }

        public Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun, CancellationToken cancellationToken)
        {
            return _firebaseMessaging.SendMulticastAsync(message, dryRun, cancellationToken);
        }

        public Task<TopicManagementResponse> SubscribeToTopicAsync(IReadOnlyList<string> registrationTokens, string topic)
        {
            return _firebaseMessaging.SubscribeToTopicAsync(registrationTokens, topic);
        }

        public Task<TopicManagementResponse> UnsubscribeFromTopicAsync(IReadOnlyList<string> registrationTokens, string topic)
        {
            return _firebaseMessaging.UnsubscribeFromTopicAsync(registrationTokens, topic);
        }
    }
}