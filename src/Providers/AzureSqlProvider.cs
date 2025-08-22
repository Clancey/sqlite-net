using System;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace SQLite.Providers
{
	internal class AzureSqlProvider : IDatabaseProvider
	{
		public string ProviderName => "Azure SQL";
		public bool SupportsInsertOrReplace => false; // Azure SQL uses MERGE instead
		public bool SupportsMultipleStatements => true;
		public bool RequiresNamedParameters => true;
		public string ParameterPrefix => "@";
		
		public bool CanHandleConnectionString(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				return false;
				
			// Check for explicit provider hint first
			if (connectionString.Contains("Provider=AzureSql"))
				return true;
				
			return connectionString.Contains("database.windows.net") ||
				   connectionString.Contains("Azure") ||
				   connectionString.Contains("Authentication=Active Directory");
		}
		
		public string TransformConnectionString(string connectionString)
		{
			// Remove the Provider parameter which is not understood by SqlClient
			var result = connectionString;
			
			// Remove Provider=AzureSql parameter
			if (result.Contains("Provider=AzureSql"))
			{
				result = result.Replace("Provider=AzureSql;", "").Replace(";Provider=AzureSql", "").Replace("Provider=AzureSql", "");
			}
			
			return result;
		}
		
		public string GetSqlType(Type clrType, int? maxLength = null)
		{
			// Azure SQL uses the same types as SQL Server
			if (clrType == typeof(bool))
				return "BIT";
			else if (clrType == typeof(byte))
				return "TINYINT";
			else if (clrType == typeof(sbyte) || clrType == typeof(short))
				return "SMALLINT";
			else if (clrType == typeof(ushort) || clrType == typeof(int))
				return "INT";
			else if (clrType == typeof(uint) || clrType == typeof(long) || clrType == typeof(ulong))
				return "BIGINT";
			else if (clrType == typeof(float))
				return "REAL";
			else if (clrType == typeof(double))
				return "FLOAT";
			else if (clrType == typeof(decimal))
				return "DECIMAL(18,2)";
			else if (clrType == typeof(string))
			{
				if (maxLength.HasValue)
					return maxLength.Value > 4000 ? "NVARCHAR(MAX)" : $"NVARCHAR({maxLength.Value})";
				return "NVARCHAR(MAX)";
			}
			else if (clrType == typeof(byte[]))
				return "VARBINARY(MAX)";
			else if (clrType == typeof(DateTime))
				return "DATETIME2";
			else if (clrType == typeof(DateTimeOffset))
				return "DATETIMEOFFSET";
			else if (clrType == typeof(TimeSpan))
				return "TIME";
			else if (clrType == typeof(Guid))
				return "UNIQUEIDENTIFIER";
			else if (clrType.IsEnum)
				return "INT";
			else
				return "NVARCHAR(MAX)"; // Default fallback
		}
		
		public string GetColumnDeclaration(string columnName, string dataType, bool isPrimaryKey, bool autoIncrement, bool isNotNull, bool isUnique, string defaultValue, string collation)
		{
			var parts = new System.Collections.Generic.List<string>();
			
			parts.Add(QuoteIdentifier(columnName));
			parts.Add(dataType);
			
			if (autoIncrement)
				parts.Add("IDENTITY(1,1)");
			
			if (isPrimaryKey)
				parts.Add("PRIMARY KEY");
			
			if (isNotNull && !isPrimaryKey) // PRIMARY KEY implies NOT NULL
				parts.Add("NOT NULL");
			else if (!isNotNull && !isPrimaryKey)
				parts.Add("NULL");
				
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
			return $"[{identifier}]";
		}
		
		public string GetLimitClause(int? limit, int? offset)
		{
			if (limit.HasValue && offset.HasValue)
				return $"OFFSET {offset.Value} ROWS FETCH NEXT {limit.Value} ROWS ONLY";
			else if (limit.HasValue)
				return $"OFFSET 0 ROWS FETCH NEXT {limit.Value} ROWS ONLY";
			else if (offset.HasValue)
				return $"OFFSET {offset.Value} ROWS";
			else
				return "";
		}
		
		public string GetRandomFunction()
		{
			return "NEWID()";
		}
		
		public string GetLastInsertIdFunction()
		{
			return "SCOPE_IDENTITY()";
		}
		
		// CREATE TABLE Support
		public string GetCreateTablePrefix(bool isVirtual, string virtualUsing)
		{
			// Azure SQL doesn't support virtual tables, and IF NOT EXISTS isn't directly supported
			// We'll need to handle existence checking differently 
			return "CREATE TABLE";
		}
		
		public string GetCreateTableSuffix(bool withoutRowId)
		{
			// Azure SQL doesn't support WITHOUT ROWID
			return "";
		}
		
		public bool SupportsIfNotExists => false;
		
		// CRUD SQL Generation - Azure SQL specific implementations (same as SQL Server)
		public string GenerateInsertSql(TableMapping table, string extra)
		{
			var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;
			var cols = replacing ? table.InsertOrReplaceColumns : table.InsertColumns;
			
			if (cols.Length == 0 && table.Columns.Length == 1 && table.Columns[0].IsAutoInc)
			{
				return string.Format("INSERT INTO {0} DEFAULT VALUES", QuoteIdentifier(table.TableName));
			}
			else
			{
				if (replacing && !SupportsInsertOrReplace)
				{
					// Azure SQL uses MERGE for upsert operations (same as SQL Server)
					var pk = table.PK;
					if (pk == null)
						throw new NotSupportedException("InsertOrReplace requires a primary key in Azure SQL");
						
					var nonPkCols = cols.Where(c => !c.IsPK).ToArray();
					var updateAssignments = nonPkCols.Select((c, i) => $"{QuoteIdentifier(c.Name)} = @param{Array.IndexOf(cols, c)}");
					var insertCols = string.Join(",", cols.Select(c => QuoteIdentifier(c.Name)));
					var insertValues = string.Join(",", cols.Select((c, i) => "@param" + i));
					
					return string.Format(@"MERGE {0} AS target
USING (SELECT {1}) AS source ({2})
ON target.{3} = source.{3}
WHEN MATCHED THEN 
	UPDATE SET {4}
WHEN NOT MATCHED THEN
	INSERT ({5}) VALUES ({6});",
						QuoteIdentifier(table.TableName),
						insertValues,
						insertCols,
						QuoteIdentifier(pk.Name),
						string.Join(",", updateAssignments),
						insertCols,
						insertValues);
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
			else if (limit.HasValue || offset.HasValue)
			{
				// Azure SQL requires ORDER BY for OFFSET/FETCH (same as SQL Server)
				var pk = table.PK;
				if (pk != null)
					sql += " ORDER BY " + QuoteIdentifier(pk.Name);
				else
					sql += " ORDER BY (SELECT NULL)"; // Fallback if no PK
			}
				
			var limitClause = GetLimitClause(limit, offset);
			if (!string.IsNullOrEmpty(limitClause))
				sql += " " + limitClause;
				
			return sql;
		}
		
		// Transaction support - Azure SQL syntax (same as SQL Server)
		public string GenerateSavepointSql(string savepointName)
		{
			return $"SAVE TRANSACTION {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateRollbackToSavepointSql(string savepointName)
		{
			return $"ROLLBACK TRANSACTION {QuoteIdentifier(savepointName)}";
		}
		
		public string GenerateReleaseSavepointSql(string savepointName)
		{
			// Azure SQL doesn't have RELEASE SAVEPOINT, savepoints are automatically released
			// when the transaction commits or when the savepoint goes out of scope
			return ""; // Return empty string to indicate no-op
		}
		
		// ADO.NET Connection Management
		public IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}
		
		public void ConfigureConnection(IDbConnection connection)
		{
			// Azure SQL specific connection configuration
			if (connection is SqlConnection sqlConn)
			{
				// Azure SQL specific settings could go here
				// For example, setting connection resiliency options
			}
		}
	}
}