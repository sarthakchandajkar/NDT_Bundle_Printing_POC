#!/bin/bash
# Quick NDT Bundle Test - Interactive
# This script guides you through testing the NDT bundle flow

echo "=========================================="
echo "NDT Bundle Tag Printing - Quick Test"
echo "=========================================="
echo ""

# Check if server is running
if ! curl -s http://localhost:5001/api/bundles > /dev/null 2>&1; then
    echo "⚠ Server is not running!"
    echo ""
    echo "Starting server in background..."
    cd "$(dirname "$0")/.."
    dotnet run --project NDTBundlePOC.UI.Web --urls "http://localhost:5001" > /dev/null 2>&1 &
    SERVER_PID=$!
    echo "Server started (PID: $SERVER_PID)"
    echo "Waiting for server to be ready..."
    sleep 5
    
    # Wait for server
    for i in {1..20}; do
        if curl -s http://localhost:5001/api/bundles > /dev/null 2>&1; then
            echo "✓ Server is ready!"
            break
        fi
        sleep 1
    done
fi

echo ""
echo "=========================================="
echo "Test Steps:"
echo "=========================================="
echo ""
echo "1. Open browser: http://localhost:5001"
echo ""
echo "2. In the UI:"
echo "   - Enter '5' in 'Add NDT Cuts' field"
echo "   - Click 'Add NDT Cuts' button"
echo "   - Wait 2 seconds"
echo "   - Click 'Add NDT Cuts' again (5 more)"
echo ""
echo "3. Verify:"
echo "   - Bundle appears in grid"
echo "   - Status shows 'Completed'"
echo "   - NDT Pcs = 10"
echo ""
echo "4. Print:"
echo "   - Click on the completed bundle row"
echo "   - Click 'Print Selected Bundle'"
echo ""
echo "5. Check output:"
echo "   - Print file: ~/Documents/NDT_Bundle_POC_Prints/"
echo "   - Excel file: ~/Documents/NDT_Bundle_POC_Exports/"
echo ""
echo "=========================================="
echo ""
read -p "Press Enter when you've completed the test..."
echo ""

# Get bundle count
BUNDLE_COUNT=$(curl -s http://localhost:5001/api/bundles | grep -o '"ndtBundle_ID"' | wc -l | tr -d ' ')
echo "Current bundles in system: $BUNDLE_COUNT"
echo ""
echo "Test complete! Check the output files mentioned above."

