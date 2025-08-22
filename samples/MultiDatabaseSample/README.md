# SQLite-NET Multi-Database Sample

This sample demonstrates the new multi-database support in SQLite-NET, allowing you to use the same familiar API with different database backends.

## Features Demonstrated

- **SQLite** - Traditional file-based database (no changes to existing code)
- **MariaDB** - MySQL-compatible database server
- **SQL Server** - Microsoft SQL Server
- **Azure SQL** - Cloud-based SQL Server variant

## Prerequisites

- .NET 8.0 SDK or later
- Docker (for running database servers)

> **Note for Mac Users**: The docker-compose uses SQL Server with x64 emulation via Rosetta 2. This works well on M1/M2 Macs but may use more resources than native containers. For production use, consider using actual Azure SQL or a Linux server.

## Quick Start

### 1. Start Database Servers (Optional)

If you want to test MariaDB and SQL Server support, start the Docker containers:

```bash
# Start both database servers
docker-compose up -d

# Or start them individually:
docker run --name mariadb-test -e MYSQL_ROOT_PASSWORD=password -e MYSQL_DATABASE=testdb -p 3307:3306 -d mariadb:latest
docker run --name sqlserver-test --platform linux/amd64 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=YourPassword123! -e MSSQL_PID=Developer -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Run the Sample

```bash
dotnet run
```

The sample will:
1. Always run the SQLite demo (no external dependencies)
2. Attempt to connect to MariaDB and SQL Server if available
3. Demonstrate CRUD operations, transactions, and queries on each database

## Connection String Examples

### SQLite (Traditional)
```csharp
var db = new SQLiteConnection("demo.db");
```

### MariaDB
```csharp
var db = new SQLiteConnection("Server=localhost;Port=3307;Database=testdb;Uid=root;Pwd=password;Provider=MariaDB");
```

### SQL Server
```csharp
var db = new SQLiteConnection("Server=localhost,1433;Database=testdb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;Provider=SqlServer");
```

### Azure SQL
```csharp
var db = new SQLiteConnection("Server=your-server.database.windows.net;Database=testdb;User Id=username;Password=password;Provider=AzureSql");
```

## Key Points

- **100% API Compatibility**: All existing SQLite-NET code works unchanged
- **Provider Detection**: Automatically detects the database type from the connection string
- **Real Connections**: Uses actual ADO.NET connections, not mock implementations
- **Transaction Support**: Full transaction support across all providers
- **Type Safety**: Maintains SQLite-NET's strong typing and LINQ support

## Sample Operations

The sample demonstrates:
- Creating tables with attributes (PrimaryKey, AutoIncrement, MaxLength, Unique)
- Inserting single and multiple records
- Querying with LINQ (Where, OrderBy, FirstOrDefault)
- Updating records
- Running transactions with multiple operations
- Aggregate queries (Count)
- Dropping tables

## Troubleshooting

### Docker containers not starting
- Ensure Docker Desktop is running
- Check port availability (3307 for MariaDB, 1433 for SQL Server)
- View logs: `docker-compose logs`

### Connection failures
- Verify containers are running: `docker ps`
- Check connection strings match your setup
- For SQL Server, ensure it accepts TCP connections

### Clean up
```bash
# Stop and remove containers
docker-compose down

# Remove containers and volumes
docker-compose down -v
```