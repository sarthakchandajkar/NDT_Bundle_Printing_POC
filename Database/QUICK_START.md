# Database Quick Start

## 📍 SQL File Location
```
Database/NDT_Database_Schema.sql
```

## ⚡ Quick Steps

1. **Open SQL Server Management Studio (SSMS)**

2. **Connect to your SQL Server**
   - Server: `localhost` or your server name
   - Authentication: Windows or SQL Server Auth

3. **Open the SQL file**
   - `File` → `Open` → `File...`
   - Select: `Database/NDT_Database_Schema.sql`

4. **Update database name** (Line 7)
   ```sql
   USE [YourDatabaseName];  -- Change to your database name
   ```

5. **Execute** (Press `F5` or click Execute)

6. **Update connection string** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "ServerConnectionString": "Server=localhost;Database=YourDatabaseName;User Id=user;Password=pass;TrustServerCertificate=True;"
   }
   ```

## ✅ Verify Tables Created

Run this query:
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('NDT_BundleFormationChart', 'M1_NDTBundles');
```

Should return 2 rows.

## 📚 Full Guide

See `DATABASE_SETUP_GUIDE.md` for detailed instructions.

