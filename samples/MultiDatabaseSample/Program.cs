using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiDatabaseSample
{
    // Sample data model
    public class Product
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class Customer
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Unique]
        public string Email { get; set; } = string.Empty;
        
        public DateTime RegisteredDate { get; set; }
        public int TotalOrders { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SQLite-NET Multi-Database Sample ===\n");

            // Run demos for each database provider
            RunSQLiteDemo();
            Console.WriteLine();
            
            RunMariaDBDemo();
            Console.WriteLine();
            
            RunSqlServerDemo();
            Console.WriteLine();
            
            RunAzureSqlDemo();
            
            Console.WriteLine("\n=== All demos completed successfully! ===");
        }

        static void RunSQLiteDemo()
        {
            Console.WriteLine("1. SQLite Demo (File-based database)");
            Console.WriteLine("----------------------------------------");

            // Traditional SQLite connection - unchanged from original API
            var db = new SQLiteConnection("demo.db");
            
            RunDatabaseOperations(db, "SQLite");
            
            db.Dispose();
            Console.WriteLine("✓ SQLite demo completed");
        }

        static void RunMariaDBDemo()
        {
            Console.WriteLine("2. MariaDB Demo (Requires Docker container on port 3307)");
            Console.WriteLine("----------------------------------------");

            try
            {
                // MariaDB connection with provider specification
                var connectionString = "Server=localhost;Port=3307;Database=testdb;Uid=root;Pwd=password;Provider=MariaDB";
                var db = new SQLiteConnection(connectionString);
                
                RunDatabaseOperations(db, "MariaDB");
                
                db.Dispose();
                Console.WriteLine("✓ MariaDB demo completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ MariaDB demo skipped: {ex.Message}");
                Console.WriteLine("  To run MariaDB demo, start the container:");
                Console.WriteLine("  docker run --name mariadb-test -e MYSQL_ROOT_PASSWORD=password -e MYSQL_DATABASE=testdb -p 3307:3306 -d mariadb:latest");
            }
        }

        static void RunSqlServerDemo()
        {
            Console.WriteLine("3. SQL Server Demo (Requires Docker container on port 1433)");
            Console.WriteLine("----------------------------------------");

            try
            {
                // SQL Server connection with provider specification
                var connectionString = "Server=localhost,1433;Database=testdb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;Provider=SqlServer";
                var db = new SQLiteConnection(connectionString);
                
                RunDatabaseOperations(db, "SQL Server");
                
                db.Dispose();
                Console.WriteLine("✓ SQL Server demo completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ SQL Server demo skipped: {ex.Message}");
                Console.WriteLine("  To run SQL Server demo, start the container:");
                Console.WriteLine("  docker run --name sqlserver-test --platform linux/amd64 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=YourPassword123! -e MSSQL_PID=Developer -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest");
            }
        }

        static void RunAzureSqlDemo()
        {
            Console.WriteLine("4. Azure SQL Demo (Uses same container as SQL Server)");
            Console.WriteLine("----------------------------------------");

            try
            {
                // Azure SQL connection (can point to actual Azure SQL or local SQL Server)
                var connectionString = "Server=localhost,1433;Database=azuredb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;Provider=AzureSql";
                var db = new SQLiteConnection(connectionString);
                
                RunDatabaseOperations(db, "Azure SQL");
                
                db.Dispose();
                Console.WriteLine("✓ Azure SQL demo completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Azure SQL demo skipped: {ex.Message}");
                Console.WriteLine("  Azure SQL uses the same container as SQL Server");
            }
        }

        static void RunDatabaseOperations(SQLiteConnection db, string providerName)
        {
            Console.WriteLine($"Running operations on {providerName}...");
            
            // Display provider info
            if (db.Provider != null)
            {
                Console.WriteLine($"  Provider: {db.Provider.ProviderName}");
                Console.WriteLine($"  Supports Insert or Replace: {db.Provider.SupportsInsertOrReplace}");
                Console.WriteLine($"  Parameter Prefix: {db.Provider.ParameterPrefix}");
            }

            // Create tables
            db.CreateTable<Product>();
            db.CreateTable<Customer>();
            Console.WriteLine("  ✓ Tables created");

            // Insert sample data
            var products = new List<Product>
            {
                new Product 
                { 
                    Name = "Laptop", 
                    Description = "High-performance laptop", 
                    Price = 999.99m, 
                    StockQuantity = 50,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new Product 
                { 
                    Name = "Mouse", 
                    Description = "Wireless mouse", 
                    Price = 29.99m, 
                    StockQuantity = 200,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                },
                new Product 
                { 
                    Name = "Keyboard", 
                    Description = "Mechanical keyboard", 
                    Price = 89.99m, 
                    StockQuantity = 0,
                    CreatedAt = DateTime.Now,
                    IsActive = false
                }
            };

            foreach (var product in products)
            {
                db.Insert(product);
            }
            Console.WriteLine($"  ✓ Inserted {products.Count} products");

            var customers = new List<Customer>
            {
                new Customer 
                { 
                    FirstName = "John", 
                    LastName = "Doe", 
                    Email = "john.doe@example.com",
                    RegisteredDate = DateTime.Now.AddDays(-30),
                    TotalOrders = 5
                },
                new Customer 
                { 
                    FirstName = "Jane", 
                    LastName = "Smith", 
                    Email = "jane.smith@example.com",
                    RegisteredDate = DateTime.Now.AddDays(-60),
                    TotalOrders = 12
                }
            };

            db.InsertAll(customers);
            Console.WriteLine($"  ✓ Inserted {customers.Count} customers");

            // Query data
            var activeProducts = db.Table<Product>()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToList();
            Console.WriteLine($"  ✓ Found {activeProducts.Count} active products");

            var topCustomer = db.Table<Customer>()
                .OrderByDescending(c => c.TotalOrders)
                .FirstOrDefault();
            if (topCustomer != null)
            {
                Console.WriteLine($"  ✓ Top customer: {topCustomer.FirstName} {topCustomer.LastName} ({topCustomer.TotalOrders} orders)");
            }

            // Update data
            var laptop = db.Table<Product>().Where(p => p.Name == "Laptop").FirstOrDefault();
            if (laptop != null)
            {
                laptop.Price = 899.99m;
                db.Update(laptop);
                Console.WriteLine("  ✓ Updated laptop price");
            }

            // Transaction example
            try
            {
                db.RunInTransaction(() =>
                {
                    var keyboard = db.Table<Product>().Where(p => p.Name == "Keyboard").FirstOrDefault();
                    if (keyboard != null)
                    {
                        keyboard.StockQuantity = 100;
                        keyboard.IsActive = true;
                        db.Update(keyboard);
                    }

                    var newCustomer = new Customer
                    {
                        FirstName = "Bob",
                        LastName = "Johnson",
                        Email = "bob.johnson@example.com",
                        RegisteredDate = DateTime.Now,
                        TotalOrders = 1
                    };
                    db.Insert(newCustomer);
                });
                Console.WriteLine("  ✓ Transaction completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Transaction failed: {ex.Message}");
            }

            // Aggregate queries
            var totalProducts = db.Table<Product>().Count();
            var totalCustomers = db.Table<Customer>().Count();
            Console.WriteLine($"  ✓ Total records: {totalProducts} products, {totalCustomers} customers");

            // Clean up (optional - comment out to keep data for inspection)
            db.DropTable<Product>();
            db.DropTable<Customer>();
            Console.WriteLine("  ✓ Tables dropped");
        }
    }
}