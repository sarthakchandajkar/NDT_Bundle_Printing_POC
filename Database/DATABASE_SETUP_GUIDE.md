# Database Setup Guide

## 📍 Where to Create SQL Tables

The SQL schema file is located at:
```
Database/NDT_Database_Schema.sql
```

## 🗄️ Database Requirements

- **Database Type**: Microsoft SQL Server (SQL Server 2012 or later)
- **Database Name**: You'll need to specify your database name
- **Permissions**: You need `CREATE TABLE`, `CREATE PROCEDURE`, and `INSERT` permissions

## 🚀 How to Execute the SQL Script

### Option 1: SQL Server Management Studio (SSMS) - Recommended

1. **Open SQL Server Management Studio**
   - Download from: https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms

2. **Connect to Your Database Server**
   - Server name: `localhost` or your SQL Server instance name
   - Authentication: Windows Authentication or SQL Server Authentication
   - Click "Connect"

3. **Open the SQL Script**
   - In SSMS, go to `File` → `Open` → `File...`
   - Navigate to: `Database/NDT_Database_Schema.sql`
   - Or copy the contents of the file

4. **Update Database Name**
   - Find this line in the script:
     ```sql
     USE [YourDatabaseName]; -- Replace with your actual database name
     ```
   - Replace `YourDatabaseName` with your actual database name
   - Example:
     ```sql
     USE [NDT_Bundle_POC];
     ```

5. **Execute the Script**
   - Click the "Execute" button (or press `F5`)
   - Check the "Messages" tab for success messages

### Option 2: Azure Data Studio (Cross-Platform)

1. **Open Azure Data Studio**
   - Download from: https://azure.microsoft.com/en-us/products/data-studio/

2. **Connect to Your Database**
   - Click "New Connection"
   - Enter server details and connect

3. **Open SQL Script**
   - `File` → `Open File...`
   - Select `Database/NDT_Database_Schema.sql`

4. **Update Database Name** (same as Option 1)

5. **Run the Script**
   - Click "Run" button or press `F5`

### Option 3: Command Line (sqlcmd)

```bash
# Windows
sqlcmd -S localhost -d YourDatabaseName -i Database\NDT_Database_Schema.sql

# macOS/Linux (if SQL Server is installed)
sqlcmd -S localhost -d YourDatabaseName -i Database/NDT_Database_Schema.sql
```

### Option 4: Visual Studio

1. **Open Server Explorer**
   - `View` → `Server Explorer`

2. **Add Data Connection**
   - Right-click "Data Connections" → "Add Connection"
   - Enter your SQL Server details

3. **Open SQL Script**
   - `File` → `Open` → `File...`
   - Select `Database/NDT_Database_Schema.sql`

4. **Execute**
   - Right-click in the SQL editor → "Execute"

## 📋 What Gets Created

The script creates:

1. **`NDT_BundleFormationChart` Table**
   - Stores NDT bundle formation rules per mill/PO
   - Columns: `NDTBundleFormationChart_ID`, `Mill_ID`, `PO_Plan_ID`, `NDT_PcsPerBundle`, etc.

2. **`M1_NDTBundles` Table**
   - Stores NDT bundle records
   - Columns: `NDTBundle_ID`, `Bundle_No`, `Batch_No`, `NDT_Pcs`, `Status`, etc.

3. **`SP_SAPData_Mill_NDTBundle` Stored Procedure**
   - Used for Excel export
   - Returns bundle data for a specific bundle number

4. **Printer Configuration**
   - Adds entry to `PlantDevice` table for NDT printer

## ⚙️ Configuration After Setup

### 1. Update Connection String

After creating the tables, you need to configure the connection string in your application.

**For .NET Core/ASP.NET Core** (current project):
Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ServerConnectionString": "Server=localhost;Database=YourDatabaseName;User Id=your_user;Password=your_password;TrustServerCertificate=True;"
  }
}
```

**For .NET Framework** (if using):
Add to `app.config` or `web.config`:

```xml
<connectionStrings>
  <add name="ServerConnectionString" 
       connectionString="Server=localhost;Database=YourDatabaseName;User Id=your_user;Password=your_password;TrustServerCertificate=True;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### 2. Update Printer IP Address

After running the script, update the printer IP address in the `PlantDevice` table:

```sql
UPDATE [dbo].[PlantDevice] 
SET [IPAddress] = '192.168.1.200'  -- Your actual printer IP
WHERE [DeviceAbbr] = 'M1NDTPrinter';
```

### 3. Insert Sample Data (Optional)

Uncomment the sample data section in the SQL script or run:

```sql
INSERT INTO [dbo].[NDT_BundleFormationChart] ([Mill_ID], [PO_Plan_ID], [NDT_PcsPerBundle], [IsActive])
VALUES 
    (1, NULL, 10, 1),  -- Default: 10 pieces per bundle for all POs in Mill 1
    (1, 1, 15, 1);      -- Specific: 15 pieces per bundle for PO_Plan_ID = 1 in Mill 1
```

## ✅ Verification

After executing the script, verify the tables were created:

```sql
-- Check if tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('NDT_BundleFormationChart', 'M1_NDTBundles');

-- Check if stored procedure exists
SELECT name 
FROM sys.procedures 
WHERE name = 'SP_SAPData_Mill_NDTBundle';

-- Check printer configuration
SELECT * FROM [dbo].[PlantDevice] WHERE [DeviceAbbr] = 'M1NDTPrinter';
```

## 🔍 Troubleshooting

### Error: "Database 'YourDatabaseName' does not exist"
- **Solution**: Create the database first:
  ```sql
  CREATE DATABASE [YourDatabaseName];
  GO
  ```

### Error: "Cannot find the object 'PO_Plan'"
- **Solution**: The script references existing tables (`PO_Plan`, `M1_Slit`). Ensure these tables exist in your database, or remove the foreign key constraints if they don't exist.

### Error: "Permission denied"
- **Solution**: Ensure your SQL user has `CREATE TABLE` and `CREATE PROCEDURE` permissions:
  ```sql
  GRANT CREATE TABLE TO [your_user];
  GRANT CREATE PROCEDURE TO [your_user];
  ```

### Error: "Invalid object name 'PlantDevice'"
- **Solution**: The `PlantDevice` table should already exist. If it doesn't, you can skip that section or create it first.

## 📝 Next Steps

1. ✅ Execute the SQL script in your database
2. ✅ Update connection string in `appsettings.json`
3. ✅ Update printer IP address in `PlantDevice` table
4. ✅ Insert NDT bundle formation chart data
5. ✅ Test the application with database connection

## 🔗 Related Files

- `Database/NDT_Database_Schema.sql` - Main SQL schema file
- `NDTBundlePOC.UI.Web/appsettings.json` - Application configuration
- `NDT_IMPLEMENTATION_SUMMARY.md` - Implementation overview

