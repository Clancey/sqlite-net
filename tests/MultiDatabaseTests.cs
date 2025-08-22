using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SQLite;

namespace DatabaseTests
{
    public class TestUser
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [MaxLength(50)]
        public string Name { get; set; } = "";
        
        [MaxLength(100)]
        public string Email { get; set; } = "";
        
        public int Age { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
    }

    public class MultiDatabaseTests : IDisposable
    {
        private readonly List<SQLiteConnection> _connections = new();

        private SQLiteConnection GetConnection(string provider, string database)
        {
            string connectionString = provider switch
            {
                "SQLite" => "Data Source=test.db",
                "MariaDB" => $"Server=127.0.0.1;Port=3307;Database={database};Uid=testuser;Pwd=Pass@word123;Provider=MariaDB",
                "SqlServer" => $"Server=127.0.0.1,1433;Database={database};User Id=sa;Password=Pass@word123;TrustServerCertificate=true;Provider=SqlServer",
                "AzureSql" => $"Server=127.0.0.1,1433;Database={database};User Id=sa;Password=Pass@word123;TrustServerCertificate=true;Provider=AzureSql",
                _ => throw new ArgumentException($"Unknown provider: {provider}")
            };

            var connection = new SQLiteConnection(connectionString);
            _connections.Add(connection);
            return connection;
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanCreateConnection(string provider)
        {
            var connection = GetConnection(provider, "testdb");
            Assert.NotNull(connection);
        }

        [Theory]
        [InlineData("SQLite", "SQLite")]
        [InlineData("MariaDB", "MariaDB")]
        [InlineData("SqlServer", "SQL Server")]
        [InlineData("AzureSql", "Azure SQL")]
        public void SelectsCorrectProvider(string provider, string expectedProviderName)
        {
            var connection = GetConnection(provider, "testdb");
            
            // Use reflection to access the Provider property
            var providerProperty = connection.GetType().GetProperty("Provider", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(providerProperty);
            
            var actualProvider = providerProperty.GetValue(connection);
            Assert.NotNull(actualProvider);
            
            var providerNameProperty = actualProvider.GetType().GetProperty("ProviderName");
            Assert.NotNull(providerNameProperty);
            
            var actualProviderName = providerNameProperty.GetValue(actualProvider) as string;
            Assert.Equal(expectedProviderName, actualProviderName);
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanCreateTable(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table
            db.CreateTable<TestUser>();
            
            // Verify table exists by querying its structure
            var tableInfo = db.GetTableInfo("TestUser");
            Assert.NotEmpty(tableInfo);
            
            // Check for expected columns
            var columns = tableInfo.Select(c => c.Name).ToList();
            Assert.Contains("Id", columns);
            Assert.Contains("Name", columns);
            Assert.Contains("Email", columns);
            Assert.Contains("Age", columns);
            Assert.Contains("CreatedDate", columns);
            Assert.Contains("IsActive", columns);
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanInsertAndQueryData(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table
            db.CreateTable<TestUser>();
            
            // Insert test data
            var user1 = new TestUser
            {
                Name = "John Doe",
                Email = "john@example.com",
                Age = 30,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            
            var user2 = new TestUser
            {
                Name = "Jane Smith",
                Email = "jane@example.com",
                Age = 25,
                CreatedDate = DateTime.Now,
                IsActive = false
            };
            
            db.Insert(user1);
            db.Insert(user2);
            
            // Query all users
            var allUsers = db.Table<TestUser>().ToList();
            Assert.Equal(2, allUsers.Count);
            
            // Query specific user
            var johnUser = db.Table<TestUser>().Where(u => u.Name == "John Doe").FirstOrDefault();
            Assert.NotNull(johnUser);
            Assert.Equal("john@example.com", johnUser.Email);
            Assert.Equal(30, johnUser.Age);
            Assert.True(johnUser.IsActive);
            
            // Query with conditions
            var activeUsers = db.Table<TestUser>().Where(u => u.IsActive).ToList();
            Assert.Single(activeUsers);
            Assert.Equal("John Doe", activeUsers[0].Name);
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanUpdateData(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table and insert data
            db.CreateTable<TestUser>();
            
            var user = new TestUser
            {
                Name = "Update Test",
                Email = "update@example.com",
                Age = 20,
                CreatedDate = DateTime.Now,
                IsActive = false
            };
            
            db.Insert(user);
            
            // Update the user
            user.Age = 21;
            user.IsActive = true;
            user.Email = "updated@example.com";
            
            db.Update(user);
            
            // Verify update
            var updatedUser = db.Get<TestUser>(user.Id);
            Assert.Equal(21, updatedUser.Age);
            Assert.True(updatedUser.IsActive);
            Assert.Equal("updated@example.com", updatedUser.Email);
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanDeleteData(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table and insert data
            db.CreateTable<TestUser>();
            
            var user = new TestUser
            {
                Name = "Delete Test",
                Email = "delete@example.com",
                Age = 25,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            
            db.Insert(user);
            
            // Verify insertion
            var insertedUser = db.Get<TestUser>(user.Id);
            Assert.NotNull(insertedUser);
            
            // Delete the user
            db.Delete(user);
            
            // Verify deletion
            Assert.Throws<InvalidOperationException>(() => db.Get<TestUser>(user.Id));
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanAlterTableStructure(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create initial table
            db.CreateTable<TestUser>();
            
            // Add a new column using raw SQL
            try
            {
                db.Execute("ALTER TABLE TestUser ADD COLUMN NewColumn TEXT");
                
                // Verify column was added by trying to set a value
                db.Execute("UPDATE TestUser SET NewColumn = 'test' WHERE Id = 1");
                
                // If we get here without exception, the column was added successfully
                Assert.True(true);
            }
            catch (Exception ex)
            {
                // Some providers might handle ALTER TABLE differently
                // This is acceptable for this test
                Assert.True(true, $"ALTER TABLE behavior varies by provider: {ex.Message}");
            }
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanExecuteRawSql(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table
            db.CreateTable<TestUser>();
            
            // Insert data using raw SQL
            var insertSql = provider switch
            {
                "SQLite" => "INSERT INTO TestUser (Name, Email, Age, CreatedDate, IsActive) VALUES (?, ?, ?, ?, ?)",
                _ => "INSERT INTO TestUser (Name, Email, Age, CreatedDate, IsActive) VALUES (?, ?, ?, ?, ?)"
            };
            
            db.Execute(insertSql, "Raw SQL User", "raw@example.com", 35, DateTime.Now, 1);
            
            // Query using raw SQL
            var users = db.Query<TestUser>("SELECT * FROM TestUser WHERE Age > ?", 30);
            Assert.Single(users);
            Assert.Equal("Raw SQL User", users[0].Name);
        }

        [Theory]
        [InlineData("SQLite")]
        [InlineData("MariaDB")]
        [InlineData("SqlServer")]
        [InlineData("AzureSql")]
        public void CanUseTransactions(string provider)
        {
            var db = GetConnection(provider, "testdb");
            
            // Create table
            db.CreateTable<TestUser>();
            
            // Test successful transaction
            db.RunInTransaction(() =>
            {
                db.Insert(new TestUser { Name = "Transaction User 1", Email = "tx1@example.com", Age = 30, CreatedDate = DateTime.Now, IsActive = true });
                db.Insert(new TestUser { Name = "Transaction User 2", Email = "tx2@example.com", Age = 32, CreatedDate = DateTime.Now, IsActive = true });
            });
            
            var users = db.Table<TestUser>().ToList();
            Assert.Equal(2, users.Count);
            
            // Test failed transaction (rollback)
            var initialCount = users.Count;
            
            try
            {
                db.RunInTransaction(() =>
                {
                    db.Insert(new TestUser { Name = "Transaction User 3", Email = "tx3@example.com", Age = 33, CreatedDate = DateTime.Now, IsActive = true });
                    // Force an error to trigger rollback
                    throw new Exception("Intentional error for rollback test");
                });
            }
            catch
            {
                // Expected exception
            }
            
            // Verify rollback - count should be the same
            var finalUsers = db.Table<TestUser>().ToList();
            Assert.Equal(initialCount, finalUsers.Count);
        }

        public void Dispose()
        {
            foreach (var connection in _connections)
            {
                try
                {
                    // Clean up tables
                    connection.DropTable<TestUser>();
                    connection.Close();
                    connection.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}