using System;
using SQLite.Providers;

namespace SQLite.AzureSql
{
    /// <summary>
    /// Initializes the Azure SQL provider for SQLite-NET
    /// </summary>
    public static class AzureSqlProviderInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the Azure SQL provider. Call this once at application startup.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                DatabaseProviderRegistry.RegisterProvider(new AzureSqlProvider());
                _initialized = true;
            }
        }

        /// <summary>
        /// Gets whether the Azure SQL provider has been initialized
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _initialized;
                }
            }
        }
    }
}