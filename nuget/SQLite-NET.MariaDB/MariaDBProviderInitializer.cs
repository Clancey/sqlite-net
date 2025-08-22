using System;
using SQLite.Providers;

namespace SQLite.MariaDB
{
    /// <summary>
    /// Initializes the MariaDB provider for SQLite-NET
    /// </summary>
    public static class MariaDBProviderInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the MariaDB provider. Call this once at application startup.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                DatabaseProviderRegistry.RegisterProvider(new MariaDBProvider());
                _initialized = true;
            }
        }

        /// <summary>
        /// Gets whether the MariaDB provider has been initialized
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