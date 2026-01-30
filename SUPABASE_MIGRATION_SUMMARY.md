# SQL Server to Supabase (PostgreSQL) Migration Summary

## ✅ Migration Complete

All code has been successfully migrated from SQL Server to Supabase (PostgreSQL).

## 📦 Packages Added

### NDTBundlePOC.Core.csproj
- ✅ `Npgsql` v8.0.5 - PostgreSQL data provider
- ✅ `System.Configuration.ConfigurationManager` v9.0.0 - For ConfigurationManager support

## 🔄 Files Updated

### 1. **NDTBundlePOC.Core/NDTBundlePOC.Core.csproj**
- Added Npgsql package reference
- Added ConfigurationManager package reference

### 2. **NDTBundlePOC.UI.Web/appsettings.json**
- Updated connection string format to Supabase PostgreSQL format
- Format: `Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;`

### 3. **PLC/CSVUtility.cs**
- ✅ Replaced `using System.Data.SqlClient;` → `using Npgsql;`
- ✅ Replaced `SqlConnection` → `NpgsqlConnection`
- ✅ Replaced `SqlCommand` → `NpgsqlCommand`
- ✅ Replaced `SqlDataReader` → `NpgsqlDataReader`
- ✅ Replaced `SqlParameter` → `NpgsqlParameter`
- ✅ Updated stored procedure call to PostgreSQL function: `SELECT * FROM "SP_SAPData_Mill_NDTBundle"(@MillId, @BundleNum)`
- ✅ Removed `CommandType.StoredProcedure` (not needed for function calls)

### 4. **PLC/NDTBundlePrintHandler.cs**
- ✅ Replaced all `SqlConnection` → `NpgsqlConnection`
- ✅ Replaced all `SqlCommand` → `NpgsqlCommand`
- ✅ Replaced all `SqlDataReader` → `NpgsqlDataReader`
- ✅ Updated all SQL queries with PostgreSQL syntax:
  - Added double quotes around all table/column names
  - Replaced `TOP 1` → `LIMIT 1`
  - Replaced `GETDATE()` → `CURRENT_TIMESTAMP`
  - Updated table references: `M{millId}_Slit` → `"M{millId}_Slit"`
  - Updated table references: `M{millId}_NDTBundles` → `"M{millId}_NDTBundles"`

### 5. **PLC/NDTBundleFormationLogic.cs**
- ✅ Replaced `using System.Data.SqlClient;` → `using Npgsql;`
- ✅ Replaced all `SqlCommand` → `NpgsqlCommand`
- ✅ Replaced all `SqlDataReader` → `NpgsqlDataReader`
- ✅ Updated all SQL queries with PostgreSQL syntax:
  - Added double quotes around all identifiers
  - Replaced `TOP 1` → `LIMIT 1`
  - Replaced `ISNULL()` → `COALESCE()`
  - Replaced `GETDATE()` → `CURRENT_TIMESTAMP`
  - Updated all table/column references with quotes

### 6. **Rpt_MillLabel.cs**
- ✅ Replaced `using System.Data.SqlClient;` → `using Npgsql;`
- ✅ Replaced `SqlConnection` → `NpgsqlConnection`
- ✅ Replaced `SqlCommand` → `NpgsqlCommand`
- ✅ Replaced `SqlDataReader` → `NpgsqlDataReader`
- ✅ Updated all SQL queries with PostgreSQL syntax:
  - Added double quotes around all identifiers
  - Replaced `isnull()` → `COALESCE()`
  - Replaced `iif()` → `CASE WHEN ... THEN ... ELSE ... END`
  - Replaced `GETDATE()` → `CURRENT_TIMESTAMP`
  - Updated all table/column references with quotes

### 7. **Rpt_MillLabel.Designer.cs**
- ✅ Replaced `using System.Data.SqlClient;` → `using Npgsql;`

## 🔧 SQL Syntax Changes Applied

### Table and Column Names
- **Before:** `M1_NDTBundles`, `Bundle_No`, `[Status]`
- **After:** `"M1_NDTBundles"`, `"Bundle_No"`, `"Status"`

### Functions
- **Before:** `ISNULL(column, default)` → **After:** `COALESCE(column, default)`
- **Before:** `iif(condition, true, false)` → **After:** `CASE WHEN condition THEN true ELSE false END`
- **Before:** `GETDATE()` → **After:** `CURRENT_TIMESTAMP`

### Query Limits
- **Before:** `SELECT TOP 1 ...` → **After:** `SELECT ... LIMIT 1`

### Stored Procedures → Functions
- **Before:** `CommandType.StoredProcedure` with `"SP_Name"`
- **After:** `SELECT * FROM "SP_Name"(@Param1, @Param2)` (direct function call)

### Parameter Syntax
- **Before:** `@ParameterName` (SQL Server)
- **After:** `@ParameterName` (Npgsql supports this syntax)

## 📋 Connection String Format

### Supabase PostgreSQL Connection String
```
Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=your_password;SSL Mode=Require;
```

### How to Get Your Supabase Connection String
1. Go to your Supabase project dashboard
2. Navigate to **Settings** → **Database**
3. Find **Connection string** section
4. Copy the **URI** or **Connection pooling** string
5. Update `appsettings.json` with your credentials

## ⚠️ Important Notes

### 1. Database Schema
- Ensure your Supabase database has the same tables as the SQL Server schema
- Table names must match exactly (case-sensitive with quotes)
- All tables should use double-quoted identifiers: `"M1_NDTBundles"`, `"PO_Plan"`, etc.

### 2. PostgreSQL Functions
- The stored procedure `SP_SAPData_Mill_NDTBundle` must be created as a PostgreSQL function in Supabase
- Function signature should match: `SP_SAPData_Mill_NDTBundle(MillId INT, BundleNum VARCHAR)`

### 3. Case Sensitivity
- PostgreSQL is case-sensitive with quoted identifiers
- All table/column names in queries use double quotes
- Ensure your Supabase schema uses the same case as the queries

### 4. ConfigurationManager
- Added `System.Configuration.ConfigurationManager` package for .NET Core compatibility
- Files using `ConfigurationManager.ConnectionStrings` will work correctly

## 🧪 Testing Checklist

- [ ] Update connection string in `appsettings.json` with your Supabase credentials
- [ ] Verify all tables exist in Supabase with correct names and columns
- [ ] Create PostgreSQL function `SP_SAPData_Mill_NDTBundle` in Supabase
- [ ] Test database connection
- [ ] Test NDT bundle formation logic
- [ ] Test NDT bundle printing
- [ ] Test CSV export functionality
- [ ] Test Rpt_MillLabel report generation

## 📝 Next Steps

1. **Update Connection String:**
   ```json
   {
     "ConnectionStrings": {
       "ServerConnectionString": "Host=db.YOUR_PROJECT.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;"
     }
   }
   ```

2. **Verify Database Schema:**
   - Ensure all tables exist: `"M1_NDTBundles"`, `"M1_Slit"`, `"PO_Plan"`, `"NDT_BundleFormationChart"`, `"PlantDevice"`
   - Verify column names match exactly (case-sensitive)

3. **Create PostgreSQL Function:**
   - Convert `SP_SAPData_Mill_NDTBundle` stored procedure to PostgreSQL function
   - Function should return a table/result set

4. **Test the Application:**
   - Run the application
   - Test PLC connection and data reading
   - Test bundle formation and printing

## ✅ Migration Status

All code files have been successfully migrated. The application is now ready to use Supabase PostgreSQL instead of SQL Server.

