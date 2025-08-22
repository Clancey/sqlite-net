using System;

namespace SQLite
{
	/// <summary>
	/// Advanced database provider factory with support for multiple database types
	/// Only available in SQLite-net-std package
	/// </summary>
	internal static partial class DatabaseProviderFactory
	{
		/// <summary>
		/// Gets the appropriate database provider for the given connection string
		/// </summary>
		private static readonly IDatabaseProvider[] _providers = new IDatabaseProvider[]
		{
			new Providers.AzureSqlProvider(), // Most specific first
			new Providers.MariaDBProvider(),
			new Providers.SqlServerProvider(), 
			new Providers.SQLiteProvider()  // Most generic last
		};

		public static IDatabaseProvider GetProvider(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
			{
				return new Providers.SQLiteProvider(); // Default to SQLite
			}
				
			foreach (var p in _providers)
			{
				if (p.CanHandleConnectionString(connectionString))
				{
					return p;
				}
			}
			
			// Default to SQLite if no provider matches
			return new Providers.SQLiteProvider();
		}
	}
}