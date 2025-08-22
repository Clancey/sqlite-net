# SQLite-NET.Core

Core abstractions and interfaces for SQLite-NET multi-database support.

## Overview

This package provides the foundational types and contracts needed to build database providers for SQLite-NET. It contains:

- `IDatabaseProvider` interface - Core contract for database providers
- `DatabaseProviderRegistry` - Registry for managing database providers
- Shared abstractions used across all providers

## Usage

This package is typically not referenced directly. Instead, reference the main `SQLite-NET.SQLite` package and any additional provider packages you need.

### For Provider Authors

If you're building a custom database provider for SQLite-NET:

1. Reference this package
2. Implement `IDatabaseProvider` interface
3. Create an initializer class to register your provider

```csharp
public class MyDatabaseProvider : IDatabaseProvider
{
    public string ProviderName => "MyDatabase";
    // ... implement all interface methods
}

public static class MyDatabaseProviderInitializer
{
    public static void Initialize()
    {
        DatabaseProviderRegistry.RegisterProvider(new MyDatabaseProvider());
    }
}
```

## Supported Providers

- **SQLite-NET.SQLite** - SQLite provider (default, includes backward compatibility)
- **SQLite-NET.MariaDB** - MariaDB/MySQL provider
- **SQLite-NET.SqlServer** - SQL Server provider
- **SQLite-NET.AzureSql** - Azure SQL provider

## Installation

This package is automatically included when you install provider packages.

```bash
# Install main package with SQLite support
dotnet add package SQLite-NET.SQLite

# Add additional providers as needed
dotnet add package SQLite-NET.MariaDB
dotnet add package SQLite-NET.SqlServer
```