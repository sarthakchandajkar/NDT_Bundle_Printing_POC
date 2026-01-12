# Telerik Reporting Setup Guide

## Overview
Telerik Reporting requires a license and is not available on the public NuGet feed. You need to configure a private NuGet feed or install the assemblies manually.

## Option 1: Using Telerik NuGet Feed (Recommended)

### Step 1: Get Telerik NuGet Feed URL
1. Log in to your Telerik account: https://www.telerik.com/account
2. Go to **Downloads** → **NuGet Feed**
3. Copy your **NuGet Feed URL** (looks like: `https://nuget.telerik.com/v3/index.json`)

### Step 2: Configure NuGet Feed

Create `NuGet.config` in the solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Telerik" value="YOUR_TELERIK_NUGET_FEED_URL" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

### Step 3: Add Credentials (if required)

If your feed requires authentication, add credentials:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Telerik" value="YOUR_TELERIK_NUGET_FEED_URL" />
  </packageSources>
  <packageSourceCredentials>
    <Telerik>
      <add key="Username" value="YOUR_TELERIK_EMAIL" />
      <add key="ClearTextPassword" value="YOUR_TELERIK_PASSWORD" />
    </Telerik>
  </packageSourceCredentials>
</configuration>
```

### Step 4: Restore Packages
```bash
dotnet restore
```

## Option 2: Manual Assembly Installation

If you have Telerik Reporting installed locally:

### Step 1: Locate Telerik Assemblies
Find these DLLs in your Telerik installation folder (typically `C:\Program Files (x86)\Progress\Telerik Reporting R# 2024\Bin`):
- `Telerik.Reporting.dll`
- `Telerik.Reporting.ImageFormat.dll`
- `Telerik.Reporting.Processing.dll`
- `Telerik.Reporting.XmlSerialization.dll`

### Step 2: Copy to Project
1. Create `lib` folder in `NDTBundlePOC.Core`
2. Copy Telerik DLLs to `lib` folder
3. Update `.csproj` to reference local assemblies

### Step 3: Update Project File

Replace the PackageReference with:

```xml
<ItemGroup>
  <Reference Include="Telerik.Reporting">
    <HintPath>lib\Telerik.Reporting.dll</HintPath>
  </Reference>
  <Reference Include="Telerik.Reporting.ImageFormat">
    <HintPath>lib\Telerik.Reporting.ImageFormat.dll</HintPath>
  </Reference>
  <Reference Include="Telerik.Reporting.Processing">
    <HintPath>lib\Telerik.Reporting.Processing.dll</HintPath>
  </Reference>
  <Reference Include="Telerik.Reporting.XmlSerialization">
    <HintPath>lib\Telerik.Reporting.XmlSerialization.dll</HintPath>
  </Reference>
</ItemGroup>
```

## Option 3: Use Report Server (If Available)

If you have Telerik Report Server installed, you can use the REST API instead of direct assembly references.

## Package Versions

The code is configured for Telerik Reporting 2024.1.130. Adjust version numbers if you have a different version:
- 2024.1.130 (latest)
- 2023.3.1014
- 2023.2.1014
- etc.

## Verification

After setup, verify installation:
```bash
dotnet build
```

If successful, you should see no errors related to Telerik.Reporting.

## Troubleshooting

### Error: Unable to find package Telerik.Reporting
- **Solution**: Configure NuGet feed or use manual assembly installation

### Error: License validation failed
- **Solution**: Ensure you have a valid Telerik Reporting license

### Error: Assembly not found
- **Solution**: Check that all required Telerik DLLs are in the correct location

