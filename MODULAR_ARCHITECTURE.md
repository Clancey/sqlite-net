# SQLite-NET Modular Architecture

## Overview

SQLite-NET now supports a modular architecture with separate NuGet packages for different database providers while maintaining 100% backward compatibility.

## Package Structure

### Core Packages

#### SQLite-NET.Core
- **Purpose**: Core abstractions and interfaces
- **Contains**: `IDatabaseProvider`, `DatabaseProviderRegistry`
- **Usage**: Usually referenced automatically by provider packages

#### SQLite-NET.SQLite  
- **Purpose**: Main package with SQLite support (default provider)
- **Contains**: Complete SQLite-NET ORM + SQLite provider
- **Backward Compatibility**: 100% - all existing code works unchanged
- **Installation**: `dotnet add package SQLite-NET.SQLite`

### Provider Packages

#### SQLite-NET.MariaDB
- **Purpose**: MariaDB/MySQL provider
- **Dependencies**: MySqlConnector, SQLite-NET.Core
- **Installation**: `dotnet add package SQLite-NET.MariaDB`

#### SQLite-NET.SqlServer
- **Purpose**: SQL Server provider  
- **Dependencies**: Microsoft.Data.SqlClient, SQLite-NET.Core
- **Installation**: `dotnet add package SQLite-NET.SqlServer`

#### SQLite-NET.AzureSql
- **Purpose**: Azure SQL provider
- **Dependencies**: Microsoft.Data.SqlClient, SQLite-NET.Core
- **Installation**: `dotnet add package SQLite-NET.AzureSql`

## Usage Patterns

### 1. SQLite Only (Backward Compatible)

```xml
<PackageReference Include="SQLite-NET.SQLite" Version="1.11.0" />
```

```csharp
using SQLite;

// Existing code works unchanged
var db = new SQLiteConnection("database.db");
db.CreateTable<User>();
```

### 2. Multi-Database Support

```xml
<PackageReference Include="SQLite-NET.SQLite" Version="1.11.0" />
<PackageReference Include="SQLite-NET.MariaDB" Version="1.11.0" />
<PackageReference Include="SQLite-NET.SqlServer" Version="1.11.0" />
```

```csharp
using SQLite;
using SQLite.MariaDB;
using SQLite.SqlServer;

// App startup - initialize providers
MariaDBProviderInitializer.Initialize();
SqlServerProviderInitializer.Initialize();

// Use different databases with same API
var sqliteDb = new SQLiteConnection("database.db");
var mariaDb = new SQLiteConnection("Server=localhost;Database=test;Uid=root;Pwd=pass;Provider=MariaDB");
var sqlServerDb = new SQLiteConnection("Server=localhost;Database=test;Provider=SqlServer;Integrated Security=true");

// Same operations work across all providers
foreach (var db in new[] { sqliteDb, mariaDb, sqlServerDb })
{
    db.CreateTable<User>();
    db.Insert(new User { Name = "Test", Email = "test@example.com" });
    var users = db.Table<User>().ToList();
}
```

### 3. Custom Provider Development

```xml
<PackageReference Include="SQLite-NET.Core" Version="1.11.0" />
<PackageReference Include="MyDatabase.Client" Version="x.x.x" />
```

```csharp
using SQLite;

public class MyDatabaseProvider : IDatabaseProvider
{
    public string ProviderName => "MyDatabase";
    public bool SupportsInsertOrReplace => true;
    public string ParameterPrefix => "@";
    
    public bool CanHandleConnectionString(string connectionString)
    {
        return connectionString.Contains("Provider=MyDatabase");
    }
    
    // Implement all interface methods...
}

public static class MyDatabaseProviderInitializer
{
    public static void Initialize()
    {
        DatabaseProviderRegistry.RegisterProvider(new MyDatabaseProvider());
    }
}
```

## Migration from Monolithic Structure

### Phase 1: Install New Packages

For existing SQLite-only usage:
```bash
# Remove old package
dotnet remove package sqlite-net-pcl

# Add new modular package
dotnet add package SQLite-NET.SQLite
```

No code changes needed - full backward compatibility maintained.

### Phase 2: Add Additional Providers

```bash
# Add provider packages as needed
dotnet add package SQLite-NET.MariaDB
dotnet add package SQLite-NET.SqlServer
```

### Phase 3: Initialize Providers

```csharp
// Application startup
using SQLite.MariaDB;
using SQLite.SqlServer;

public class Startup
{
    public void ConfigureServices()
    {
        // Initialize providers once at startup
        MariaDBProviderInitializer.Initialize();
        SqlServerProviderInitializer.Initialize();
    }
}
```

### Phase 4: Use Multi-Database Connections

```csharp
// Configuration-driven database selection
var connectionString = Configuration.GetConnectionString("Database");

// Automatic provider detection based on connection string
var db = new SQLiteConnection(connectionString);
```

## Benefits

### For Users
- **Choose what you need** - Only install providers you actually use
- **Smaller bundle sizes** - Avoid unused database client dependencies
- **Better separation of concerns** - Clear boundaries between providers
- **Easier testing** - Mock or swap providers easily
- **Backward compatibility** - Existing code works unchanged

### For Maintainers  
- **Modular development** - Work on providers independently
- **Reduced complexity** - Each provider is self-contained
- **Independent versioning** - Update providers separately
- **Better testing** - Test providers in isolation
- **Community contributions** - Easier to add new providers

### For Enterprise
- **Dependency management** - Control which database clients are included
- **Security auditing** - Audit only the providers you use
- **License compliance** - Clear license boundaries per provider
- **Deployment flexibility** - Different apps can use different providers

## Development Workflow

### Building All Packages

```bash
# Build core package
dotnet build nuget/SQLite-NET.Core/

# Build main package  
dotnet build nuget/SQLite-NET.SQLite/

# Build provider packages
dotnet build nuget/SQLite-NET.MariaDB/
dotnet build nuget/SQLite-NET.SqlServer/
dotnet build nuget/SQLite-NET.AzureSql/
```

### Testing

```bash
# Test with specific providers
dotnet test tests/ --filter "Category=SQLite"
dotnet test tests/ --filter "Category=MariaDB"
dotnet test tests/ --filter "Category=SqlServer"
```

### Publishing

```bash
# Pack all packages
dotnet pack nuget/SQLite-NET.Core/ -c Release
dotnet pack nuget/SQLite-NET.SQLite/ -c Release  
dotnet pack nuget/SQLite-NET.MariaDB/ -c Release
dotnet pack nuget/SQLite-NET.SqlServer/ -c Release
dotnet pack nuget/SQLite-NET.AzureSql/ -c Release

# Publish to NuGet
dotnet nuget push nuget/SQLite-NET.Core/bin/Release/*.nupkg
dotnet nuget push nuget/SQLite-NET.SQLite/bin/Release/*.nupkg
# etc...
```

## Versioning Strategy

- **Major versions** - Breaking changes to core interfaces
- **Minor versions** - New providers or features
- **Patch versions** - Bug fixes and improvements

All packages maintain synchronized version numbers for consistency.

## Roadmap

### Phase 1 (Current)
- ✅ Core abstractions package
- ✅ SQLite provider package (with backward compatibility)
- ✅ MariaDB provider package
- ✅ SQL Server provider package  
- ✅ Azure SQL provider package

### Phase 2 (Future)
- PostgreSQL provider package
- Oracle provider package
- MySQL provider (distinct from MariaDB)
- Firebird provider package

### Phase 3 (Advanced)
- Provider-specific optimizations
- Advanced caching strategies
- Connection pooling providers
- Monitoring and diagnostics providers

## Contributing

### Adding New Providers

1. Create new package directory: `nuget/SQLite-NET.YourProvider/`
2. Implement `IDatabaseProvider` interface
3. Create provider initializer class
4. Add comprehensive tests
5. Document connection string format and usage
6. Submit pull request

### Provider Requirements

- Must implement all `IDatabaseProvider` methods
- Must handle connection string detection properly
- Must generate database-specific SQL correctly
- Must include comprehensive unit tests
- Must include integration tests with real database
- Must document supported types and limitations

## Support

- **Issues**: [GitHub Issues](https://github.com/praeclarum/sqlite-net/issues)
- **Discussions**: [GitHub Discussions](https://github.com/praeclarum/sqlite-net/discussions)
- **Documentation**: [Wiki](https://github.com/praeclarum/sqlite-net/wiki)

## License

All packages maintain the same MIT license as the original SQLite-NET project.