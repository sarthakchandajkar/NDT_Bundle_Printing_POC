# Supabase Connection Troubleshooting Guide

## Error: "No such host is known"

If you're getting this error, it means the DNS cannot resolve your Supabase hostname. Here's how to fix it:

## Step 1: Verify Your Supabase Connection String

1. **Go to Supabase Dashboard**
   - Visit: https://app.supabase.com
   - Log in to your account

2. **Navigate to Your Project**
   - Select your project from the dashboard

3. **Get the Connection String**
   - Go to **Settings** → **Database**
   - Scroll down to **Connection string** section
   - Select **URI** or **Connection pooling** tab
   - Copy the connection string

4. **Verify the Hostname Format**
   - The hostname should look like: `db.xxxxxxxxxxxxx.supabase.co`
   - It should NOT have any extra characters or spaces
   - Example: `Host=db.abcdefghijklm.supabase.co;Port=5432;...`

## Step 2: Check Your Project Status

1. **Verify Project is Active**
   - Go to **Settings** → **General**
   - Check if project status is "Active"
   - If it shows "Paused", you need to resume it

2. **Check Project Reference ID**
   - The hostname contains your project reference ID
   - Verify it matches what's in your Supabase dashboard

## Step 3: Test DNS Resolution

### On Windows (PowerShell):
```powershell
nslookup db.oomeyszjgfxuxocrnhua.supabase.co
# or
Resolve-DnsName db.oomeyszjgfxuxocrnhua.supabase.co
```

### On macOS/Linux:
```bash
nslookup db.oomeyszjgfxuxocrnhua.supabase.co
# or
dig db.oomeyszjgfxuxocrnhua.supabase.co
# or
host db.oomeyszjgfxuxocrnhua.supabase.co
```

**Expected Result:** Should return an IP address (e.g., `54.xxx.xxx.xxx`)

**If it fails:** The hostname is incorrect or the project doesn't exist

## Step 4: Common Issues and Solutions

### Issue 1: Hostname is Incorrect
**Solution:** 
- Copy the connection string directly from Supabase dashboard
- Don't type it manually - use copy/paste
- Check for typos or extra spaces

### Issue 2: Project is Paused
**Solution:**
- Go to Supabase Dashboard → Settings → General
- Click "Resume Project" if it's paused
- Free tier projects auto-pause after 1 week of inactivity

### Issue 3: Project was Deleted
**Solution:**
- Create a new Supabase project
- Get the new connection string
- Update `appsettings.json` with the new connection string

### Issue 4: Network/Firewall Blocking
**Solution:**
- Check if your network allows outbound connections to port 5432
- Try from a different network (mobile hotspot, etc.)
- Check corporate firewall settings

### Issue 5: Connection String Format
**Solution:**
- Ensure the format is correct:
  ```
  Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;
  ```
- Note: Use semicolons (`;`) not commas
- Use `SSL Mode=Require;` (with semicolon at the end)

## Step 5: Alternative Connection Methods

### Option 1: Use Connection Pooling
Supabase provides connection pooling which uses a different hostname:
- Go to Settings → Database → Connection pooling
- Use the connection pooling string instead
- Format: `Host=pooler.supabase.com;Port=6543;...`

### Option 2: Use Direct Connection with IP
If DNS is the issue, you can try:
1. Get the IP address from Supabase dashboard
2. Use IP instead of hostname (not recommended for production)

## Step 6: Update appsettings.json

Once you have the correct connection string:

1. Open `NDTBundlePOC.UI.Web/appsettings.json`
2. Update the `ServerConnectionString`:
   ```json
   {
     "ConnectionStrings": {
       "ServerConnectionString": "Host=db.YOUR_PROJECT_REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;"
     }
   }
   ```
3. Replace `YOUR_PROJECT_REF` with your actual project reference
4. Replace `YOUR_PASSWORD` with your actual database password

## Step 7: Verify Connection

After updating, restart your application and check the console output. You should see:
```
→ Database connection configured for host: db.xxxxx.supabase.co
✓ DNS resolution successful. IP addresses: xxx.xxx.xxx.xxx
```

If you still see DNS errors, the hostname is incorrect or the project doesn't exist.

## Still Having Issues?

1. **Double-check the hostname** - It's the most common issue
2. **Verify project is active** - Check Supabase dashboard
3. **Try connection pooling** - Sometimes more reliable
4. **Contact Supabase support** - If project exists but hostname doesn't resolve

