using System;
using System.Data;

namespace SQLite
{
    /// <summary>
    /// Interface for database providers that implement database-specific functionality
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Name of the database provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Whether this provider supports INSERT OR REPLACE syntax
        /// </summary>
        bool SupportsInsertOrReplace { get; }

        /// <summary>
        /// Whether this provider supports multiple statements in a single command
        /// </summary>
        bool SupportsMultipleStatements { get; }

        /// <summary>
        /// Whether this provider requires named parameters
        /// </summary>
        bool RequiresNamedParameters { get; }

        /// <summary>
        /// The parameter prefix used by this provider (e.g., "@", "?", ":")
        /// </summary>
        string ParameterPrefix { get; }

        /// <summary>
        /// Whether this provider supports IF NOT EXISTS in CREATE TABLE
        /// </summary>
        bool SupportsIfNotExists { get; }

        /// <summary>
        /// Determines if this provider can handle the given connection string
        /// </summary>
        /// <param name="connectionString">Connection string to evaluate</param>
        /// <returns>True if this provider can handle the connection string</returns>
        bool CanHandleConnectionString(string connectionString);

        /// <summary>
        /// Transforms a connection string to be compatible with this provider
        /// </summary>
        /// <param name="connectionString">Original connection string</param>
        /// <returns>Transformed connection string</returns>
        string TransformConnectionString(string connectionString);

        /// <summary>
        /// Gets the SQL type name for a given CLR type
        /// </summary>
        /// <param name="clrType">CLR type to map</param>
        /// <param name="maxLength">Optional maximum length for string types</param>
        /// <returns>SQL type name</returns>
        string GetSqlType(Type clrType, int? maxLength = null);

        /// <summary>
        /// Generates a column declaration for CREATE TABLE statements
        /// </summary>
        string GetColumnDeclaration(string columnName, string dataType, bool isPrimaryKey, bool autoIncrement, bool isNotNull, bool isUnique, string defaultValue, string collation);

        /// <summary>
        /// Quotes an identifier (table name, column name) for this database
        /// </summary>
        /// <param name="identifier">Identifier to quote</param>
        /// <returns>Quoted identifier</returns>
        string QuoteIdentifier(string identifier);

        /// <summary>
        /// Gets the LIMIT/OFFSET clause for this database
        /// </summary>
        string GetLimitClause(int? limit, int? offset);

        /// <summary>
        /// Gets the random function name for this database
        /// </summary>
        string GetRandomFunction();

        /// <summary>
        /// Gets the last insert ID function for this database
        /// </summary>
        string GetLastInsertIdFunction();

        /// <summary>
        /// Gets the CREATE TABLE prefix for this database
        /// </summary>
        string GetCreateTablePrefix(bool isVirtual, string virtualUsing);

        /// <summary>
        /// Gets the CREATE TABLE suffix for this database
        /// </summary>
        string GetCreateTableSuffix(bool withoutRowId);

        /// <summary>
        /// Generates INSERT SQL for this database
        /// </summary>
        string GenerateInsertSql(TableMapping table, string extra);

        /// <summary>
        /// Generates UPDATE SQL for this database
        /// </summary>
        string GenerateUpdateSql(TableMapping table);

        /// <summary>
        /// Generates DELETE SQL for this database
        /// </summary>
        string GenerateDeleteSql(TableMapping table);

        /// <summary>
        /// Generates SELECT SQL for this database
        /// </summary>
        string GenerateSelectSql(TableMapping table, string where = null, string orderBy = null, int? limit = null, int? offset = null);

        /// <summary>
        /// Generates savepoint SQL for transactions
        /// </summary>
        string GenerateSavepointSql(string savepointName);

        /// <summary>
        /// Generates rollback to savepoint SQL
        /// </summary>
        string GenerateRollbackToSavepointSql(string savepointName);

        /// <summary>
        /// Generates release savepoint SQL
        /// </summary>
        string GenerateReleaseSavepointSql(string savepointName);

        /// <summary>
        /// Creates a database connection for this provider
        /// </summary>
        /// <param name="connectionString">Connection string</param>
        /// <returns>Database connection</returns>
        IDbConnection CreateConnection(string connectionString);

        /// <summary>
        /// Configures a database connection for this provider
        /// </summary>
        /// <param name="connection">Connection to configure</param>
        void ConfigureConnection(IDbConnection connection);
    }

    // TableMapping class will be provided by the main SQLite-NET package
}