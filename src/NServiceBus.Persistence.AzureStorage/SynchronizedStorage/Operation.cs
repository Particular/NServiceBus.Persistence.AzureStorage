﻿using System.Runtime.ExceptionServices;

namespace NServiceBus.Persistence.AzureStorage
{
    using Microsoft.Azure.Cosmos.Table;

    abstract class Operation
    {
        protected Operation(TableEntityPartitionKey partitionKey)
        {
            PartitionKey = partitionKey;
        }

        public TableEntityPartitionKey PartitionKey { get; }

        public abstract CloudTable Apply(TableBatchOperation transactionalBatch);

        public virtual void Success(TableResult result)
        {
        }

        public virtual void Conflict(TableResult result)
        {
        }

        public virtual bool Handle(StorageException storageException)
        {
            return false;
        }
    }
}