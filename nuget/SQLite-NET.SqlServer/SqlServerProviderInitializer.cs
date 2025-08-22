using System;
using SQLite.Providers;

namespace SQLite.SqlServer
{
    /// <summary>
    /// Initializes the SQL Server provider for SQLite-NET
    /// </summary>
    public static class SqlServerProviderInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the SQL Server provider. Call this once at application startup.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                DatabaseProviderRegistry.RegisterProvider(new SqlServerProvider());
                _initialized = true;
            }
        }

        /// <summary>
        /// Gets whether the SQL Server provider has been initialized
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