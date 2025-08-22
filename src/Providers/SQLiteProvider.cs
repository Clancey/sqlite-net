using System;
using System.Data;
using System.Linq;

namespace SQLite.Providers
{
	internal class SQLiteProvider : IDatabaseProvider
	{
		public string ProviderName => "SQLite";
		public bool SupportsInsertOrReplace => true;
		public bool SupportsMultipleStatements => true;
		public bool RequiresNamedParameters => false;
		public string ParameterPrefix => "?";
		
		public bool CanHandleConnectionString(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				return true; // Default to SQLite for empty connection strings
				
			// Check for file-based connection strings or explicit SQLite indicators
			return connectionString.Contains(".db") || 
				   connectionString.Contains(".sqlite") ||
				   connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ||
				   connectionString.StartsWith("Database=", StringComparison.OrdinalIgnoreCase) ||
				   connectionString.Contains("Version=3") ||
				   !connectionString.Contains("Server=") && !connectionString.Contains("Host=");
		}
		
		public string TransformConnectionString(string connectionString)
		{
			// SQLite connection strings don't need transformation
			return connectionString;
		}
		
		public string GetSqlType(Type clrType, int? maxLength = null)
		{
			if (clrType == typeof(bool) || clrType == typeof(byte) || clrType == typeof(ushort) || 
				clrType == typeof(sbyte) || clrType == typeof(short) || clrType == typeof(int) ||
				clrType == typeof(uint) || clrType == typeof(long) || clrType == typeof(ulong))
			{
				return "INTEGER";
			}
			else if (clrType == typeof(float) || clrType == typeof(double) || clrType == typeof(decimal))
			{
				return "REAL";
			}
			else if (clrType == typeof(string))
			{
				if (maxLength.HasValue)
					return $"VARCHAR({maxLength.Value})";
				return "TEXT";
			}
			else if (clrType == typeof(byte[]))
			{
				return "BLOB";
			}
			else if (clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset))
			{
				return "TEXT"; // SQLite stores dates as text by default
			}
			else if (clrType == typeof(TimeSpan))
			{
				return "INTEGER"; // Store as ticks
			}
			else if (clrType == typeof(Guid))
			{
				return "TEXT";
			}
			else if (clrType.IsEnum)
			{
				return "INTEGER";
			}
			else
			{
				return "TEXT"; // Default fallback
			}
		}
		
		public string GetColumnDeclaration(string columnName, string dataType, bool isPrimaryKey, bool autoIncrement, bool isNotNull, bool isUnique, string defaultValue, string collation)
		{
			var parts = new System.Collections.Generic.List<string>();
			
			parts.Add(QuoteIdentifier(columnName));
			parts.Add(dataType);
			
			if (isPrimaryKey)
			{
				parts.Add("PRIMARY KEY");
				if (autoIncrement)
					parts.Add("AUTOINCREMENT");
			}
			
			if (isNotNull && !isPrimaryKey) // PRIMARY KEY implies NOT NULL
				parts.Add("NOT NULL");
				
			if (isUnique && !isPrimaryKey) // PRIMARY KEY implies UNIQUE
				parts.Add("UNIQUE");
				
			if (!string.IsNullOrEmpty(defaultValue))
				parts.Add($"DEFAULT {defaultValue}");
				
			if (!string.IsNullOrEmpty(collation))
				parts.Add($"COLLATE {collation}");
			
			return string.Join(" ", parts);
		}
		
		public string QuoteIdentifier(string identifier)
		{
			return $"\"{identifier}\"";
		}
		
		public string GetLimitClause(int? limit, int? offset)
		{
			if (limit.HasValue && offset.HasValue)
				return $"LIMIT {limit.Value} OFFSET {offset.Value}";
			else if (limit.HasValue)
				return $"LIMIT {limit.Value}";
			else if (offset.HasValue)
				return $"LIMIT -1 OFFSET {offset.Value}";
			else
				return "";
		}
		
		public string GetRandomFunction()
		{
			return "RANDOM()";
		}
		
		public string GetLastInsertIdFunction()
		{
			return "last_insert_rowid()";
		}
		
		// CREATE TABLE Support
		public string GetCreateTablePrefix(bool isVirtual, string virtualUsing)
		{
			if (isVirtual)
				return $"CREATE VIRTUAL TABLE IF NOT EXISTS";
			return "CREATE TABLE IF NOT EXISTS";
		}
		
		public string GetCreateTableSuffix(bool withoutRowId)
		{
			return withoutRowId ? " WITHOUT ROWID" : "";
		}
		
		public bool SupportsIfNotExists => true;
		
		// CRUD SQL Generation - SQLite specific implementations
		public string GenerateInsertSql(TableMapping table, string extra)
		{
			var cols = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0 
				? table.InsertOrReplaceColumns : table.InsertColumns;
			
			if (cols.Length == 0 && table.Columns.Length == 1 && table.Columns[0].IsAutoInc)
			{
				return string.Format("insert {1} into \"{0}\" default values", table.TableName, extra);
			}
			else
			{
				return string.Format("insert {3} into \"{0}\"({1}) values ({2})", 
					table.TableName,
					string.Join(",", cols.Select(c => "\"" + c.Name + "\"")),
					string.Join(",", cols.Select(c => "?")),
					extra);
			}
		}
		
		public string GenerateUpdateSql(TableMapping table)
		{
			var pk = table.PK;
			if (pk == null)
				throw new NotSupportedException("Cannot update objects without a primary key");
				
			var columns = table.Columns.Where(c => !c.IsPK).ToArray();
			return string.Format("update \"{0}\" set {1} where \"{2}\" = ?",
				table.TableName,
				string.Join(",", columns.Select(c => "\"" + c.Name + "\" = ?")),
				pk.Name);
		}
		
		public string GenerateDeleteSql(TableMapping table)
		{
			var pk = table.PK;
			if (pk == null)
				throw new NotSupportedException("Cannot delete objects without a primary key");
				
			return string.Format("delete from \"{0}\" where \"{1}\" = ?", table.TableName, pk.Name);
		}
		
		public string GenerateSelectSql(TableMapping table, string where = null, string orderBy = null, int? limit = null, int? offset = null)
		{
			var sql = string.Format("select * from \"{0}\"", table.TableName);
			
			if (!string.IsNullOrEmpty(where))
				sql += " where " + where;
				
			if (!string.IsNullOrEmpty(orderBy))
				sql += " order by " + orderBy;
				
			var limitClause = GetLimitClause(limit, offset);
			if (!string.IsNullOrEmpty(limitClause))
				sql += " " + limitClause;
				
			return sql;
		}
		
		// Transaction support - SQLite syntax
		public string GenerateSavepointSql(string savepointName)
		{
			return $"savepoint {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateRollbackToSavepointSql(string savepointName)
		{
			return $"rollback to {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateReleaseSavepointSql(string savepointName)
		{
			return $"release {QuoteIdentifier(savepointName)}";
		}
		
		// ADO.NET Connection Management
		public IDbConnection CreateConnection(string connectionString)
		{
			// For SQLite provider, we don't create ADO.NET connections since we use SQLite3 directly
			// This method should not be called for SQLite provider
			throw new NotSupportedException("SQLiteProvider uses SQLite3 native API, not ADO.NET connections");
		}
		
		public void ConfigureConnection(IDbConnection connection)
		{
			// Not applicable for SQLite provider
			throw new NotSupportedException("SQLiteProvider uses SQLite3 native API, not ADO.NET connections");
		}
	}
}