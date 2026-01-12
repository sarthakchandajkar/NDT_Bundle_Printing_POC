#!/bin/bash
# NDT Bundle Flow Test Script
# This script tests the complete NDT bundle formation and printing flow

echo "=========================================="
echo "NDT Bundle Tag Printing - Test Script"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost:5001"
TEST_MILL_ID=1

# Function to print test result
print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓${NC} $2"
    else
        echo -e "${RED}✗${NC} $2"
    fi
}

# Function to wait for server
wait_for_server() {
    echo "Waiting for server to start..."
    for i in {1..30}; do
        if curl -s "$BASE_URL/api/bundles" > /dev/null 2>&1; then
            echo -e "${GREEN}✓${NC} Server is running"
            return 0
        fi
        sleep 1
    done
    echo -e "${RED}✗${NC} Server did not start"
    return 1
}

# Test 1: Check server is running
echo "Test 1: Checking server status..."
if wait_for_server; then
    print_result 0 "Server is accessible"
else
    print_result 1 "Server is not accessible"
    echo "Please start the server first:"
    echo "  dotnet run --project NDTBundlePOC.UI.Web --urls \"$BASE_URL\""
    exit 1
fi
echo ""

# Test 2: Get initial bundle count
echo "Test 2: Getting initial bundle count..."
INITIAL_COUNT=$(curl -s "$BASE_URL/api/bundles" | grep -o '"ndtBundle_ID"' | wc -l | tr -d ' ')
echo "  Initial bundles: $INITIAL_COUNT"
print_result 0 "Retrieved bundle count"
echo ""

# Test 3: Add NDT cuts
echo "Test 3: Adding NDT cuts (5 pieces)..."
RESPONSE=$(curl -s -X POST "$BASE_URL/api/ndt-cuts?cuts=5")
if echo "$RESPONSE" | grep -q "success"; then
    print_result 0 "NDT cuts added successfully"
else
    print_result 1 "Failed to add NDT cuts"
    echo "  Response: $RESPONSE"
fi
echo ""

# Test 4: Verify bundle created
echo "Test 4: Verifying bundle creation..."
sleep 2
NEW_COUNT=$(curl -s "$BASE_URL/api/bundles" | grep -o '"ndtBundle_ID"' | wc -l | tr -d ' ')
if [ "$NEW_COUNT" -gt "$INITIAL_COUNT" ]; then
    print_result 0 "Bundle created (Count: $INITIAL_COUNT → $NEW_COUNT)"
else
    print_result 1 "Bundle not created (Count: $INITIAL_COUNT → $NEW_COUNT)"
fi
echo ""

# Test 5: Add more cuts to complete bundle
echo "Test 5: Adding more NDT cuts to complete bundle (5 more pieces)..."
RESPONSE=$(curl -s -X POST "$BASE_URL/api/ndt-cuts?cuts=5")
if echo "$RESPONSE" | grep -q "success"; then
    print_result 0 "Additional NDT cuts added"
else
    print_result 1 "Failed to add additional cuts"
fi
echo ""

# Test 6: Get completed bundles
echo "Test 6: Getting bundles ready for printing..."
sleep 2
BUNDLES=$(curl -s "$BASE_URL/api/bundles")
COMPLETED_COUNT=$(echo "$BUNDLES" | grep -o '"status":2' | wc -l | tr -d ' ')
if [ "$COMPLETED_COUNT" -gt 0 ]; then
    print_result 0 "Found $COMPLETED_COUNT completed bundle(s)"
    # Extract first bundle ID
    FIRST_BUNDLE_ID=$(echo "$BUNDLES" | grep -o '"ndtBundle_ID":[0-9]*' | head -1 | grep -o '[0-9]*')
    echo "  First bundle ID: $FIRST_BUNDLE_ID"
else
    print_result 1 "No completed bundles found"
    FIRST_BUNDLE_ID=""
fi
echo ""

# Test 7: Print bundle (if available)
if [ -n "$FIRST_BUNDLE_ID" ]; then
    echo "Test 7: Printing bundle (ID: $FIRST_BUNDLE_ID)..."
    PRINT_RESPONSE=$(curl -s -X POST "$BASE_URL/api/print/$FIRST_BUNDLE_ID")
    if echo "$PRINT_RESPONSE" | grep -q "success"; then
        print_result 0 "Bundle printed successfully"
        echo "  Response: $PRINT_RESPONSE"
    else
        print_result 1 "Failed to print bundle"
        echo "  Response: $PRINT_RESPONSE"
    fi
else
    echo -e "${YELLOW}⚠${NC} Test 7: Skipped (no completed bundles)"
fi
echo ""

# Test 8: Check PLC connection (simulated)
echo "Test 8: Testing PLC connection (simulated)..."
PLC_CONNECT=$(curl -s -X POST "$BASE_URL/api/plc/connect?ipAddress=192.168.1.100&rack=0&slot=1")
if echo "$PLC_CONNECT" | grep -q "success"; then
    print_result 0 "PLC connection simulated"
else
    print_result 1 "PLC connection failed"
fi

PLC_STATUS=$(curl -s "$BASE_URL/api/plc/status")
echo "  PLC Status: $PLC_STATUS"
echo ""

# Test 9: Process cuts from PLC
echo "Test 9: Processing cuts from PLC..."
PLC_CUTS=$(curl -s -X POST "$BASE_URL/api/plc/process-cuts/$TEST_MILL_ID")
if echo "$PLC_CUTS" | grep -q "success"; then
    print_result 0 "Cuts processed from PLC"
else
    print_result 1 "Failed to process cuts from PLC"
fi
echo ""

# Summary
echo "=========================================="
echo "Test Summary"
echo "=========================================="
echo "Initial bundles: $INITIAL_COUNT"
echo "Final bundles: $NEW_COUNT"
echo "Completed bundles: $COMPLETED_COUNT"
echo ""
echo "Check output files:"
echo "  - Print files: ~/Documents/NDT_Bundle_POC_Prints/"
echo "  - Excel exports: ~/Documents/NDT_Bundle_POC_Exports/"
echo ""
echo "=========================================="

