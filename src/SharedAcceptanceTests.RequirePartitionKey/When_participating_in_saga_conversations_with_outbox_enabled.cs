namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Microsoft.Azure.Cosmos.Table;
    using Persistence.AzureTable;
    using Pipeline;
    using System.Linq;
    using System.Net;
    using NServiceBus.Sagas;

    public class When_participating_in_saga_conversations_with_outbox_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_optionally_provide_transactionality_based_on_sagaid()
        {
            var correlationPropertyValue = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new ContinueSagaMessage
                {
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey(myTableRowKey);

            Assert.IsNotNull(myEntity);
            Assert.AreEqual(context.SagaId.ToString(), myEntity["Data"].StringValue);
        }

        [Test]
        public async Task Should_optionally_provide_transactionality_based_on_sagaheader()
        {
            var correlationPropertyValue = Guid.NewGuid();
            var myTableRowKey = Guid.NewGuid();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithSagaThatWasMigrated>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = correlationPropertyValue,
                    TableRowKey = myTableRowKey
                })))
                .Done(c => c.SagaIsDone && c.HandlerIsDone)
                .Run();

            var myEntity = GetByRowKey(myTableRowKey);

            Assert.IsNotNull(myEntity);
            Assert.AreEqual(context.SagaId.ToString(), myEntity["Data"].StringValue);
        }

        private static DynamicTableEntity GetByRowKey(Guid sagaId)
        {
            var table = SetupFixture.Table;

            // table scan but still probably the easiest way to do it, otherwise we would have to take the partition key into account which complicates things because this test is shared
            var query = new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, sagaId.ToString()));

            try
            {
                var tableEntity = table.ExecuteQuery(query).FirstOrDefault();
                return tableEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        public class Context : ScenarioContext
        {
            public bool SagaIsDone { get; set; }
            public bool HandlerIsDone { get; set; }
            public Guid SagaId { get; set; }
        }

        public class EndpointWithSagaThatWasMigrated : EndpointConfigurationBuilder
        {
            public EndpointWithSagaThatWasMigrated()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableOutbox();
                    c.Pipeline.Register(typeof(PartitionPartionKeyCleanerBehavior),
                        "Cleans partition keys out");
                    c.Pipeline.Register(new ProvidePartitionKeyBasedOnSagaIdBehavior.Registration());
                });
            }

            class PartitionPartionKeyCleanerBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>,
                IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
            {
                public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();
                    return next(context);
                }

                public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();
                    return next(context);
                }
            }

            class ProvidePartitionKeyBasedOnSagaIdBehavior : Behavior<IIncomingLogicalMessageContext>
            {
                private IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId;

                public ProvidePartitionKeyBasedOnSagaIdBehavior(IProvidePartitionKeyFromSagaId providePartitionKeyFromSagaId)
                {
                    this.providePartitionKeyFromSagaId = providePartitionKeyFromSagaId;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    // to make it work in all test projects
                    context.Extensions.Remove<TableEntityPartitionKey>();

                    if (context.Message.Instance is ContinueSagaMessage continueSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<CustomSagaData>(context, new SagaCorrelationProperty(nameof(continueSagaMessage.SomeId), continueSagaMessage.SomeId))
                            .ConfigureAwait(false);
                    }

                    if (context.Message.Instance is StartSagaMessage startSagaMessage)
                    {
                        await providePartitionKeyFromSagaId
                            .SetPartitionKey<CustomSagaData>(context, new SagaCorrelationProperty(nameof(startSagaMessage.SomeId), startSagaMessage.SomeId))
                            .ConfigureAwait(false);
                    }

                    await next().ConfigureAwait(false);
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base(nameof(ProvidePartitionKeyBasedOnSagaIdBehavior),
                        typeof(ProvidePartitionKeyBasedOnSagaIdBehavior),
                        "Populates the partition key")
                    {
                        InsertBeforeIfExists(nameof(LogicalOutboxBehavior));
                    }
                }
            }

            public class CustomSaga : Saga<CustomSagaData>, IAmStartedByMessages<StartSagaMessage>, IAmStartedByMessages<ContinueSagaMessage>
            {
                public CustomSaga(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    var options = new SendOptions();
                    options.SetHeader(Headers.SagaId, Data.Id.ToString());
                    options.RouteToThisEndpoint();

                    return context.Send(new ContinueSagaMessage { SomeId = message.SomeId, TableRowKey = message.TableRowKey }, options);
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;

                    testContext.SagaId = Data.Id;
                    testContext.SagaIsDone = true;
                    return Task.CompletedTask;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<ContinueSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                private readonly Context testContext;
            }

            public class ContinueMessageHandler : IHandleMessages<ContinueSagaMessage>
            {
                public ContinueMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(ContinueSagaMessage message, IMessageHandlerContext context)
                {
                    var session = context.SynchronizedStorageSession.AzureTablePersistenceSession();

                    var entity = new MyTableEntity
                    {
                        RowKey = message.TableRowKey.ToString(),
                        PartitionKey = session.PartitionKey,
                        Data = session.PartitionKey
                    };
                    session.Batch.Add(TableOperation.Insert(entity));
                    testContext.HandlerIsDone = true;
                    return Task.CompletedTask;
                }

                private Context testContext;
            }

            public class MyTableEntity : TableEntity
            {
                public string Data { get; set; }
            }

            public class CustomSagaData : IContainSagaData
            {
                public Guid Id { get; set; }
                public string Originator { get; set; }
                public string OriginalMessageId { get; set; }
                public Guid SomeId { get; set; }
            }
        }

        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
            public Guid TableRowKey { get; set; }
        }

        public class ContinueSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

            public Guid TableRowKey { get; set; }
        }
    }
}