namespace NServiceBus.Persistence.AzureTable
{
    using System;
    using System.Threading.Tasks;
    using Installation;
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;
    using Features;

    class SubscriptionStorageInstaller : INeedToInstallSomething
    {
        public SubscriptionStorageInstaller(IServiceProvider serviceProvider, ReadOnlySettings settings)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
        }

        public async Task Install(string identity)
        {
            if (!settings.IsFeatureActive(typeof(SubscriptionStorage)))
            {
                return;
            }

            var installerSettings = settings.Get<SubscriptionStorageInstallerSettings>();
            if (installerSettings.Disabled)
            {
                return;
            }

            try
            {
                Logger.Info("Creating Subscription Table");
                await CreateTableIfNotExists(installerSettings, serviceProvider.GetRequiredService<IProvideCloudTableClientForSubscriptions>()).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Could not complete the installation. ", e);
                throw;
            }
        }

        async Task CreateTableIfNotExists(SubscriptionStorageInstallerSettings installerSettings, IProvideCloudTableClientForSubscriptions clientProvider)
        {
            var cloudTable = clientProvider.Client.GetTableReference(installerSettings.TableName);
            await cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        IServiceProvider serviceProvider;

        static readonly ILog Logger = LogManager.GetLogger<SynchronizedStorageInstaller>();
        ReadOnlySettings settings;
    }
}