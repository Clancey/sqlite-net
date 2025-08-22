# SQLite-NET.SQLite

The main SQLite-NET ORM package with multi-database provider support.

## Overview

SQLite-NET is a powerful, cross-platform, and easy-to-use ORM for SQLite databases. This package maintains 100% backward compatibility while adding support for pluggable database providers.

## Features

- **Full Backward Compatibility** - All existing SQLite-NET code works unchanged
- **Multi-Database Support** - Use the same API with different database providers
- **Cross-Platform** - Works on .NET, Xamarin, Unity, and more
- **Easy to Use** - Simple, intuitive API
- **High Performance** - Fast and efficient

## Installation

```bash
dotnet add package SQLite-NET.SQLite
```

## Basic Usage (SQLite)

```csharp
using SQLite;

// Traditional SQLite usage (unchanged)
var db = new SQLiteConnection("database.db");

// Create table
db.CreateTable<User>();

// Insert data
db.Insert(new User { Name = "John", Email = "john@example.com" });

// Query data
var users = db.Table<User>().Where(u => u.Name.Contains("John")).ToList();
```

## Multi-Database Usage

### MariaDB

First install the MariaDB provider:
```bash
dotnet add package SQLite-NET.MariaDB
```

Then initialize and use:
```csharp
using SQLite;
using SQLite.MariaDB;

// Initialize MariaDB provider
MariaDBProviderInitializer.Initialize();

// Use with MariaDB
var connectionString = "Server=localhost;Database=mydb;Uid=root;Pwd=password;Provider=MariaDB";
var db = new SQLiteConnection(connectionString);

// Same API works with MariaDB!
db.CreateTable<User>();
db.Insert(new User { Name = "Jane", Email = "jane@example.com" });
var users = db.Table<User>().ToList();
```

### SQL Server

```bash
dotnet add package SQLite-NET.SqlServer
```

```csharp
using SQLite;
using SQLite.SqlServer;

// Initialize SQL Server provider
SqlServerProviderInitializer.Initialize();

// Use with SQL Server
var connectionString = "Server=localhost;Database=mydb;Provider=SqlServer;Integrated Security=true";
var db = new SQLiteConnection(connectionString);

// Same API works with SQL Server!
db.CreateTable<User>();
```

## Available Providers

- **SQLite** (included) - File-based SQLite databases
- **SQLite-NET.MariaDB** - MariaDB/MySQL databases  
- **SQLite-NET.SqlServer** - SQL Server databases
- **SQLite-NET.AzureSql** - Azure SQL databases

## Migration Guide

### From Pure SQLite

No changes needed! Your existing code continues to work:

```csharp
// This still works exactly the same
var db = new SQLiteConnection("database.db");
```

### Adding Multi-Database Support

1. Install provider packages
2. Initialize providers at app startup
3. Use connection strings with `Provider=` parameter

```csharp
// App startup
MariaDBProviderInitializer.Initialize();
SqlServerProviderInitializer.Initialize();

// Use different databases with same code
var sqliteDb = new SQLiteConnection("database.db");
var mariaDb = new SQLiteConnection("Server=localhost;Database=test;Uid=root;Pwd=pass;Provider=MariaDB");
var sqlServerDb = new SQLiteConnection("Server=localhost;Database=test;Provider=SqlServer;Integrated Security=true");
```

## Connection String Formats

### SQLite (Default)
```
database.db
Data Source=database.db
:memory:
```

### MariaDB
```
Server=localhost;Port=3306;Database=mydb;Uid=user;Pwd=password;Provider=MariaDB
```

### SQL Server
```
Server=localhost;Database=mydb;Provider=SqlServer;Integrated Security=true
Server=localhost;Database=mydb;Provider=SqlServer;User Id=user;Password=password
```

### Azure SQL
```
Server=myserver.database.windows.net;Database=mydb;Provider=AzureSql;User Id=user;Password=password
```

## Documentation

- [API Documentation](https://github.com/praeclarum/sqlite-net/wiki)
- [Migration Guide](https://github.com/praeclarum/sqlite-net/wiki/Migration)
- [Provider Development](https://github.com/praeclarum/sqlite-net/wiki/Providers)

## License

MIT License - see LICENSE file for details.