# NDT Bundle POC - Quick Setup Script

Write-Host "Creating NDT Bundle POC Project..." -ForegroundColor Green

# Create solution
dotnet new sln -n NDTBundlePOC

# Create projects
dotnet new classlib -n NDTBundlePOC.Core -f net472
dotnet new winforms -n NDTBundlePOC.UI -f net472

# Add to solution
dotnet sln add NDTBundlePOC.Core/NDTBundlePOC.Core.csproj
dotnet sln add NDTBundlePOC.UI/NDTBundlePOC.UI.csproj

# Add references
dotnet add NDTBundlePOC.UI/NDTBundlePOC.UI.csproj reference NDTBundlePOC.Core/NDTBundlePOC.Core.csproj

# Install packages
dotnet add NDTBundlePOC.Core/NDTBundlePOC.Core.csproj package Newtonsoft.Json --version 13.0.3
dotnet add NDTBundlePOC.Core/NDTBundlePOC.Core.csproj package EPPlus --version 6.2.10

Write-Host "✓ Project structure created!" -ForegroundColor Green
Write-Host "Now copy the code files as described in the guide." -ForegroundColor Yellow

