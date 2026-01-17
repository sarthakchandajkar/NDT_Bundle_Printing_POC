#!/bin/bash
# Script to run the NDT Bundle POC project

echo "Checking for .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found!"
    echo ""
    echo "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo ""
    echo "Or if you're using Visual Studio, you can run the project from there:"
    echo "1. Open NDTBundlePOC.sln in Visual Studio"
    echo "2. Set NDTBundlePOC.UI as the startup project"
    echo "3. Press F5 to run"
    exit 1
fi

echo "Building project..."
dotnet build NDTBundlePOC.sln

if [ $? -eq 0 ]; then
    echo ""
    echo "Build successful! Starting application..."
    echo ""
    cd NDTBundlePOC.UI
    dotnet run
else
    echo "Build failed. Please check the errors above."
    exit 1
fi
