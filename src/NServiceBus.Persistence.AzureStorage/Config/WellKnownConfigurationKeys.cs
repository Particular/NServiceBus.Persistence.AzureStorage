﻿namespace NServiceBus.Persistence.AzureStorage.Config
{
    static class WellKnownConfigurationKeys
    {
        public const string SagaStorageConnectionString = "AzureSagaStorage.ConnectionString";
        public const string SagaStorageCreateSchema = "AzureSagaStorage.CreateSchema";
        public const string SagaStorageAssumeSecondaryIndicesExist = "AzureSagaStorage.SagaStorageAssumeSecondaryIndicesExist";
        public const string SubscriptionStorageConnectionString = "AzureSubscriptionStorage.ConnectionString";
        public const string SubscriptionStorageTableName = "AzureSubscriptionStorage.TableName";
        public const string SubscriptionStorageCacheFor = "AzureSubscriptionStorage.CacheFor";
        public const string SubscriptionStorageCreateSchema = "AzureSubscriptionStorage.CreateSchema";
    }
}