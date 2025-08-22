using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLite
{
    /// <summary>
    /// Registry for database providers
    /// </summary>
    public static class DatabaseProviderRegistry
    {
        private static readonly List<IDatabaseProvider> _providers = new List<IDatabaseProvider>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a database provider
        /// </summary>
        /// <param name="provider">Provider to register</param>
        public static void RegisterProvider(IDatabaseProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_lock)
            {
                // Remove any existing provider with the same name
                _providers.RemoveAll(p => p.ProviderName.Equals(provider.ProviderName, StringComparison.OrdinalIgnoreCase));
                _providers.Add(provider);
            }
        }

        /// <summary>
        /// Gets a provider that can handle the given connection string
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Compatible provider or null if none found</returns>
        public static IDatabaseProvider GetProvider(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;

            lock (_lock)
            {
                return _providers.FirstOrDefault(provider => provider.CanHandleConnectionString(connectionString));
            }
        }

        /// <summary>
        /// Gets all registered providers
        /// </summary>
        /// <returns>Array of registered providers</returns>
        public static IDatabaseProvider[] GetAllProviders()
        {
            lock (_lock)
            {
                return _providers.ToArray();
            }
        }

        /// <summary>
        /// Gets a provider by name
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <returns>Provider with the given name or null if not found</returns>
        public static IDatabaseProvider GetProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                return null;

            lock (_lock)
            {
                return _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Clears all registered providers (useful for testing)
        /// </summary>
        public static void ClearProviders()
        {
            lock (_lock)
            {
                _providers.Clear();
            }
        }
    }
}