﻿namespace NServiceBus.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using PubSub;

    static class SubscriptionBehaviorExtensions
    {
        public static void OnEndpointSubscribed<TContext>(this EndpointConfiguration b, Action<SubscriptionEventArgs, TContext> action) where TContext : ScenarioContext
        {
            b.Pipeline.Register<SubscriptionBehavior<TContext>.Registration>();

            b.RegisterComponents(c => c.ConfigureComponent(builder =>
            {
                var context = builder.Build<TContext>();
                return new SubscriptionBehavior<TContext>(action, context);
            }, DependencyLifecycle.InstancePerCall));
        }
    }
}