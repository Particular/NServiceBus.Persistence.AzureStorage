namespace NServiceBus.Persistence.AzureStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.WindowsAzure.Storage;

    static class SafeLinqExtensions
    {
        public static TSource SafeFirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) return default(TSource);

            try
            {
                return source.FirstOrDefault();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404) return default(TSource);

                throw;
            }
        }
    }
}