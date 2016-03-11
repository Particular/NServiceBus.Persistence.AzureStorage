﻿namespace NServiceBus.AzureStoragePersistence.ComponentTests.Subscriptions
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Routing;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_unsubscribing
    {
        public void Setup()
        {
            SuscriptionTestHelper.PerformStorageCleanup();
        }

        [Test]
        public async void the_subscription_should_be_removed()
        {
            var persister = SuscriptionTestHelper.CreateAzureSubscriptionStorage();
            var messageType = new MessageType(typeof(TestMessage));
            var messageTypes = new [] { messageType };

            var subscriber = new Subscriber("address://test-queue", new EndpointName("endpointName"));
            await persister.Subscribe(subscriber, messageType, null);

            var subscribers = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

            Assert.That(subscribers.Count(), Is.EqualTo(1));

            var subscription = subscribers.ToArray()[0];
            Assert.That(subscription.Endpoint, Is.EqualTo(subscriber.Endpoint));
            Assert.That(subscription.TransportAddress, Is.EqualTo(subscriber.TransportAddress));

            await persister.Unsubscribe(subscriber, messageType, null);
            var postUnsubscribe = await persister.GetSubscriberAddressesForMessage(messageTypes, null);

            Assert.That(postUnsubscribe.Count(), Is.EqualTo(0));
        }
    }
}