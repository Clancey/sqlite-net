using System;
using System.Data;
using System.Linq;
using MySqlConnector;

namespace SQLite.Providers
{
	internal class MariaDBProvider : IDatabaseProvider
	{
		public string ProviderName => "MariaDB";
		public bool SupportsInsertOrReplace => true; // MariaDB supports INSERT ... ON DUPLICATE KEY UPDATE
		public bool SupportsMultipleStatements => true;
		public bool RequiresNamedParameters => true;
		public string ParameterPrefix => "@";
		
		public bool CanHandleConnectionString(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				return false;
				
			// Check for explicit provider hint first
			if (connectionString.Contains("Provider=MariaDB"))
				return true;
				
			return connectionString.Contains("Host=") ||
				   connectionString.Contains("Port=3306") ||
				   connectionString.Contains("Port=3307") ||
				   connectionString.Contains("Uid=") ||
				   connectionString.Contains("CharSet=");
		}
		
		public string TransformConnectionString(string connectionString)
		{
			// Remove the Provider parameter which is not understood by MySqlConnector
			var result = connectionString;
			
			// Remove Provider=MariaDB parameter
			if (result.Contains("Provider=MariaDB"))
			{
				result = result.Replace("Provider=MariaDB;", "").Replace(";Provider=MariaDB", "").Replace("Provider=MariaDB", "");
			}
			
			return result;
		}
		
		public string GetSqlType(Type clrType, int? maxLength = null)
		{
			if (clrType == typeof(bool))
				return "BOOLEAN";
			else if (clrType == typeof(byte))
				return "TINYINT UNSIGNED";
			else if (clrType == typeof(sbyte))
				return "TINYINT";
			else if (clrType == typeof(short))
				return "SMALLINT";
			else if (clrType == typeof(ushort))
				return "SMALLINT UNSIGNED";
			else if (clrType == typeof(int))
				return "INT";
			else if (clrType == typeof(uint))
				return "INT UNSIGNED";
			else if (clrType == typeof(long))
				return "BIGINT";
			else if (clrType == typeof(ulong))
				return "BIGINT UNSIGNED";
			else if (clrType == typeof(float))
				return "FLOAT";
			else if (clrType == typeof(double))
				return "DOUBLE";
			else if (clrType == typeof(decimal))
				return "DECIMAL(18,2)";
			else if (clrType == typeof(string))
			{
				if (maxLength.HasValue)
					return maxLength.Value > 65535 ? "LONGTEXT" : $"VARCHAR({maxLength.Value})";
				return "LONGTEXT";
			}
			else if (clrType == typeof(byte[]))
				return "LONGBLOB";
			else if (clrType == typeof(DateTime))
				return "DATETIME";
			else if (clrType == typeof(DateTimeOffset))
				return "TIMESTAMP"; // MariaDB doesn't have native DateTimeOffset
			else if (clrType == typeof(TimeSpan))
				return "TIME";
			else if (clrType == typeof(Guid))
				return "CHAR(36)"; // Store as string representation
			else if (clrType.IsEnum)
				return "INT";
			else
				return "LONGTEXT"; // Default fallback
		}
		
		public string GetColumnDeclaration(string columnName, string dataType, bool isPrimaryKey, bool autoIncrement, bool isNotNull, bool isUnique, string defaultValue, string collation)
		{
			var parts = new System.Collections.Generic.List<string>();
			
			parts.Add(QuoteIdentifier(columnName));
			parts.Add(dataType);
			
			if (autoIncrement)
				parts.Add("AUTO_INCREMENT");
			
			if (isNotNull || isPrimaryKey) // PRIMARY KEY implies NOT NULL
				parts.Add("NOT NULL");
			
			if (isPrimaryKey)
				parts.Add("PRIMARY KEY");
				
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
			return $"`{identifier}`";
		}
		
		public string GetLimitClause(int? limit, int? offset)
		{
			if (limit.HasValue && offset.HasValue)
				return $"LIMIT {offset.Value}, {limit.Value}";
			else if (limit.HasValue)
				return $"LIMIT {limit.Value}";
			else if (offset.HasValue)
				return $"LIMIT {offset.Value}, 18446744073709551615"; // Max value for MySQL/MariaDB
			else
				return "";
		}
		
		public string GetRandomFunction()
		{
			return "RAND()";
		}
		
		public string GetLastInsertIdFunction()
		{
			return "LAST_INSERT_ID()";
		}
		
		// CREATE TABLE Support
		public string GetCreateTablePrefix(bool isVirtual, string virtualUsing)
		{
			// MariaDB doesn't support virtual tables in the same way as SQLite
			return "CREATE TABLE IF NOT EXISTS";
		}
		
		public string GetCreateTableSuffix(bool withoutRowId)
		{
			// MariaDB doesn't support WITHOUT ROWID
			return "";
		}
		
		public bool SupportsIfNotExists => true;
		
		// CRUD SQL Generation - MariaDB specific implementations
		public string GenerateInsertSql(TableMapping table, string extra)
		{
			var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
			var cols = replacing ? table.InsertOrReplaceColumns : table.InsertColumns;
			
			if (cols.Length == 0 && table.Columns.Length == 1 && table.Columns[0].IsAutoInc)
			{
				return string.Format("INSERT INTO {0} () VALUES ()", QuoteIdentifier(table.TableName));
			}
			else
			{
				if (replacing && SupportsInsertOrReplace)
				{
					// Use INSERT ... ON DUPLICATE KEY UPDATE for MariaDB
					var updateClauses = cols.Where(c => !c.IsPK).Select(c => $"{QuoteIdentifier(c.Name)} = VALUES({QuoteIdentifier(c.Name)})");
					return string.Format("INSERT INTO {0}({1}) VALUES ({2}) ON DUPLICATE KEY UPDATE {3}", 
						QuoteIdentifier(table.TableName),
						string.Join(",", cols.Select(c => QuoteIdentifier(c.Name))),
						string.Join(",", cols.Select((c, i) => "@param" + i)),
						string.Join(",", updateClauses));
				}
				else
				{
					return string.Format("INSERT INTO {0}({1}) VALUES ({2})", 
						QuoteIdentifier(table.TableName),
						string.Join(",", cols.Select(c => QuoteIdentifier(c.Name))),
						string.Join(",", cols.Select((c, i) => "@param" + i)));
				}
			}
		}
		
		public string GenerateUpdateSql(TableMapping table)
		{
			var pk = table.PK;
			if (pk == null)
				throw new NotSupportedException("Cannot update objects without a primary key");
				
			var columns = table.Columns.Where(c => !c.IsPK).ToArray();
			return string.Format("UPDATE {0} SET {1} WHERE {2} = @param{3}",
				QuoteIdentifier(table.TableName),
				string.Join(",", columns.Select((c, i) => QuoteIdentifier(c.Name) + " = @param" + i)),
				QuoteIdentifier(pk.Name),
				columns.Length);
		}
		
		public string GenerateDeleteSql(TableMapping table)
		{
			var pk = table.PK;
			if (pk == null)
				throw new NotSupportedException("Cannot delete objects without a primary key");
				
			return string.Format("DELETE FROM {0} WHERE {1} = @param0", 
				QuoteIdentifier(table.TableName), 
				QuoteIdentifier(pk.Name));
		}
		
		public string GenerateSelectSql(TableMapping table, string where = null, string orderBy = null, int? limit = null, int? offset = null)
		{
			var sql = string.Format("SELECT * FROM {0}", QuoteIdentifier(table.TableName));
			
			if (!string.IsNullOrEmpty(where))
				sql += " WHERE " + where;
				
			if (!string.IsNullOrEmpty(orderBy))
				sql += " ORDER BY " + orderBy;
				
			var limitClause = GetLimitClause(limit, offset);
			if (!string.IsNullOrEmpty(limitClause))
				sql += " " + limitClause;
				
			return sql;
		}
		
		// Transaction support - MariaDB/MySQL syntax
		public string GenerateSavepointSql(string savepointName)
		{
			return $"SAVEPOINT {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateRollbackToSavepointSql(string savepointName)
		{
			return $"ROLLBACK TO SAVEPOINT {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateReleaseSavepointSql(string savepointName)
		{
			return $"RELEASE SAVEPOINT {QuoteIdentifier(savepointName)}";
		}
		
		// ADO.NET Connection Management
		public IDbConnection CreateConnection(string connectionString)
		{
			return new MySqlConnection(connectionString);
		}
		
		public void ConfigureConnection(IDbConnection connection)
		{
			// MariaDB/MySQL specific connection configuration
			if (connection is MySqlConnection mysqlConn)
			{
				// Configure connection as needed
				// Note: Command timeout is set on individual commands, not the connection
			}
		}
	}
}