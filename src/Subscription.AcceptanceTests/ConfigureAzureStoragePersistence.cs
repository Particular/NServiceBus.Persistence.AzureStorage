﻿using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions;
using NServiceBus.Persistence.AzureStorage.Testing;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

public class ConfigureEndpointAzureStoragePersistence : IConfigureEndpointTestExecution
{
    static string ConnectionString => Utilities.GetEnvConfiguredConnectionStringForPersistence();

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var subscriptionPersistence = configuration.UsePersistence<AzureStoragePersistence, StorageType.Subscriptions>();
        subscriptionPersistence.ConnectionString(ConnectionString);
        subscriptionPersistence.DefaultTable(SetupFixture.TableName);

        var recoverabilitySettings = configuration.Recoverability();

        if (endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher1))
            || endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher2))
            || endpointName != Conventions.EndpointNamingConvention(typeof(When_multi_subscribing_to_a_polymorphic_event.Publisher2)))
        {
            recoverabilitySettings.Immediate(c => c.NumberOfRetries(1));
        }

        return Task.FromResult(0);
    }

    Task IConfigureEndpointTestExecution.Cleanup()
    {
        return Task.FromResult(0);
    }
}