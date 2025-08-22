# SQLite-NET.MariaDB

MariaDB database provider for SQLite-NET ORM.

## Overview

This package allows you to use the familiar SQLite-NET API with MariaDB databases. Install this alongside the main `SQLite-NET.SQLite` package to enable MariaDB support.

## Installation

```bash
# Install main SQLite-NET package
dotnet add package SQLite-NET.SQLite

# Install MariaDB provider
dotnet add package SQLite-NET.MariaDB
```

## Usage

```csharp
using SQLite;
using SQLite.MariaDB;

// Initialize the MariaDB provider (do this once at app startup)
MariaDBProviderInitializer.Initialize();

// Connect to MariaDB using SQLite-NET API
var connectionString = "Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=password;Provider=MariaDB";
var db = new SQLiteConnection(connectionString);

// Use the same API you know and love!
db.CreateTable<User>();
db.Insert(new User { Name = "John", Email = "john@example.com" });

var users = db.Table<User>()
    .Where(u => u.Name.Contains("John"))
    .OrderBy(u => u.Email)
    .ToList();
```

## Connection String Format

```
Server=hostname;Port=3306;Database=dbname;Uid=username;Pwd=password;Provider=MariaDB
```

### Parameters:
- **Server** - MariaDB server hostname
- **Port** - Port number (default: 3306)
- **Database** - Database name
- **Uid** - Username
- **Pwd** - Password
- **Provider=MariaDB** - Required to identify this as a MariaDB connection

## Docker Setup

For development, you can run MariaDB in Docker:

```bash
docker run --name mariadb-dev \
  -e MYSQL_ROOT_PASSWORD=password \
  -e MYSQL_DATABASE=testdb \
  -p 3306:3306 \
  -d mariadb:latest
```

Then connect with:
```csharp
var connectionString = "Server=localhost;Port=3306;Database=testdb;Uid=root;Pwd=password;Provider=MariaDB";
```

## Features

- **Full SQLite-NET API compatibility**
- **Automatic database creation** if it doesn't exist
- **MariaDB-specific SQL generation** (backtick identifiers, INSERT...ON DUPLICATE KEY UPDATE, etc.)
- **Parameter binding** with proper MariaDB parameter syntax
- **Transaction support** with MariaDB savepoints
- **Type mapping** optimized for MariaDB data types

## Supported Types

| .NET Type | MariaDB Type |
|-----------|--------------|
| `bool` | `BOOLEAN` |
| `byte` | `TINYINT UNSIGNED` |
| `short` | `SMALLINT` |
| `int` | `INT` |
| `long` | `BIGINT` |
| `float` | `FLOAT` |
| `double` | `DOUBLE` |
| `decimal` | `DECIMAL(18,2)` |
| `string` | `VARCHAR(n)` or `LONGTEXT` |
| `byte[]` | `LONGBLOB` |
| `DateTime` | `DATETIME` |
| `DateTimeOffset` | `TIMESTAMP` |
| `TimeSpan` | `TIME` |
| `Guid` | `CHAR(36)` |
| `Enum` | `INT` |

## Limitations

- MariaDB doesn't support `WITHOUT ROWID` tables
- Virtual tables are not supported in the same way as SQLite
- Some SQLite-specific functions may not be available

## Dependencies

- `MySqlConnector` - High-performance MySQL/MariaDB connector
- `SQLite-NET.Core` - Core abstractions

## License

MIT License - same as SQLite-NET