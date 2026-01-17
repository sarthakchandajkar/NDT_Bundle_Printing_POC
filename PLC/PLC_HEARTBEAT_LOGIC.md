# PLC Heartbeat Logic Implementation Guide

## Overview
This document provides the correct implementation for the PLC heartbeat counter at `DB1.DBW6` (L1_Heart_Beat, INT).

## Problem Statement
The current implementation incorrectly toggles between values 1 and 127, skipping all intermediate values. This prevents proper monitoring of PLC activity.

## Required Behavior
The heartbeat value must increment continuously from 1 to 127, then reset to 1:

```
1 → 2 → 3 → ... → 126 → 127 → 1 → repeat
```

## Implementation

### Siemens S7-1200/1500 (SCL - Structured Control Language)

Create or update a function block or organization block that runs on each PLC scan cycle:

```scl
FUNCTION_BLOCK FB_HeartbeatMonitor
VAR_INPUT
END_VAR
VAR_OUTPUT
END_VAR
VAR
    HeartbeatValue : INT;  // Current heartbeat value
END_VAR

BEGIN
    // Increment heartbeat counter
    IF HeartbeatValue < 127 THEN
        HeartbeatValue := HeartbeatValue + 1;
    ELSE
        HeartbeatValue := 1;  // Reset to 1 after reaching 127
    END_IF;
    
    // Write to DB1.DBW6
    "DB1".L1_Heart_Beat := HeartbeatValue;
END_FUNCTION_BLOCK
```

### Alternative: Using Organization Block (OB1 - Main Cycle)

If you prefer to implement directly in the main cycle:

```scl
// In OB1 (Main Organization Block)
VAR
    HeartbeatValue : INT;
END_VAR

BEGIN
    // Read current value from DB1
    HeartbeatValue := "DB1".L1_Heart_Beat;
    
    // Increment logic
    IF HeartbeatValue < 127 THEN
        HeartbeatValue := HeartbeatValue + 1;
    ELSE
        HeartbeatValue := 1;
    END_IF;
    
    // Write back to DB1.DBW6
    "DB1".L1_Heart_Beat := HeartbeatValue;
END
```

### Using Timer-Based Increment (Recommended for Production)

For more controlled timing, use a timer to increment at a fixed interval (e.g., every 100ms):

```scl
FUNCTION_BLOCK FB_HeartbeatMonitor
VAR_INPUT
END_VAR
VAR_OUTPUT
END_VAR
VAR
    HeartbeatValue : INT;
    HeartbeatTimer : TON;  // Timer On-Delay
    TimerInterval : TIME := T#100MS;  // Increment every 100ms
END_VAR

BEGIN
    // Start timer
    HeartbeatTimer(IN := TRUE, PT := TimerInterval);
    
    // Increment on timer output
    IF HeartbeatTimer.Q THEN
        IF HeartbeatValue < 127 THEN
            HeartbeatValue := HeartbeatValue + 1;
        ELSE
            HeartbeatValue := 1;
        END_IF;
        
        // Write to DB1.DBW6
        "DB1".L1_Heart_Beat := HeartbeatValue;
        
        // Reset timer
        HeartbeatTimer(IN := FALSE);
    END_IF;
END_FUNCTION_BLOCK
```

## Data Block Structure (DB1)

Ensure your DB1 data block has the following structure:

```
DB1
├── L1_Heart_Beat (INT) at offset 6 (DBW6)
└── ... (other variables)
```

## Initialization

On first scan or PLC startup, initialize the heartbeat value:

```scl
// In OB100 (Startup Organization Block) or first scan
"DB1".L1_Heart_Beat := 1;
```

## Safety Considerations

1. **Thread Safety**: The logic runs in a single scan cycle, so no race conditions
2. **Persistence**: The value is stored in DB1, which persists across scan cycles
3. **Range Check**: Always ensure the value stays within 1-127 range
4. **Initialization**: Initialize to 1 on PLC startup to avoid undefined behavior

## Testing

1. **Monitor DB1.DBW6** in TIA Portal or via HMI
2. **Verify** the value increments: 1 → 2 → 3 → ... → 127 → 1
3. **Check timing**: Value should increment at the configured interval
4. **Verify reset**: After 127, the next value should be 1

## Integration with External Systems

External systems (like this C# application) will:
- Read `DB1.DBW6` at regular intervals (e.g., every 750ms)
- Interpret values 1-127 as "ONLINE" (PLC is alive and responding)
- Interpret value -1 or out-of-range as "OFFLINE" (PLC not connected or error)
- Monitor the changing value to confirm PLC is actively running

## Troubleshooting

### Value Not Incrementing
- Check if the function block is being called in OB1
- Verify DB1 is not being overwritten by other logic
- Check for write conflicts from other programs

### Value Stuck at 1 or 127
- Verify the increment logic is executing
- Check for conditional logic that might prevent execution
- Ensure DB1 is not read-only

### Value Jumps Randomly
- Check for multiple write operations to DB1.DBW6
- Verify no other code is writing to this address
- Check for scan cycle timing issues

## Notes

- **Do NOT** toggle directly between constants (e.g., `IF value = 1 THEN value := 127`)
- **Do NOT** use random values or external triggers
- **DO** use a simple increment with reset logic
- **DO** ensure the logic executes on every scan or at a fixed interval
- The heartbeat must visibly change over time for monitoring systems to detect activity

## Example: Complete Implementation

```scl
// Organization Block: OB1 (Main Cycle)
PROGRAM Main
VAR
    HeartbeatCounter : INT;
END_VAR

BEGIN
    // Read current heartbeat value
    HeartbeatCounter := "DB1".L1_Heart_Beat;
    
    // Initialize if zero or invalid
    IF HeartbeatCounter < 1 OR HeartbeatCounter > 127 THEN
        HeartbeatCounter := 1;
    END_IF;
    
    // Increment logic
    IF HeartbeatCounter < 127 THEN
        HeartbeatCounter := HeartbeatCounter + 1;
    ELSE
        HeartbeatCounter := 1;
    END_IF;
    
    // Write back to DB1.DBW6
    "DB1".L1_Heart_Beat := HeartbeatCounter;
END_PROGRAM
```

This implementation ensures:
- ✅ Continuous counting from 1 to 127
- ✅ Automatic reset to 1 after 127
- ✅ Safe across PLC scan cycles
- ✅ Persistent storage in DB1.DBW6
- ✅ Simple and deterministic logic

