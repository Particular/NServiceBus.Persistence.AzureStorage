﻿namespace NServiceBus.Persistence.AzureStorage
{
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using Outbox;

    class OutboxStorage : Feature
    {
        internal OutboxStorage()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<SynchronizedStorage>();
            });
            DependsOn<SynchronizedStorage>();
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<IOutboxStorage, OutboxPersister>();
            context.Services.AddTransient(provider => new LogicalOutboxBehavior(provider.GetRequiredService<TableHolderResolver>()));

            context.Pipeline.Register(provider => provider.GetRequiredService<LogicalOutboxBehavior>(), "Behavior that mimics the outbox as part of the logical stage.");
        }
    }
}