# Smart Factory Simulator

A realistic data generator for Microsoft Fabric demonstrations that simulates a complete smart manufacturing environment. It continuously generates streaming telemetry data for orders flowing through a virtual factory production line and sends it to a Fabric Eventstream.

## Overview

The Smart Factory Simulator is part of the [Fabric Demo](../README.md) ecosystem. It serves as a **data seeder** that generates realistic manufacturing data and streams it to a Fabric Eventstream, enabling downstream analytics, real-time monitoring, and AI/ML scenarios.

> For environment preparation and Fabric workspace setup, see the [How To Setup Guide](../HowToSetup.md).

The simulator models a typical smart factory scenario where:
1. **Customers place orders** for aerospace manufacturing components
2. **Virtual factories spin up** to process each order
3. **Production lines generate telemetry** as orders move through various manufacturing stations
4. **Real-time data streams** to Fabric Eventstream for processing and analysis

## What It Simulates

### Order Lifecycle
Each session simulates a complete order-to-delivery workflow:

1. **Order Creation**: A customer places an order for a specific product (e.g., Turbine Blade Assembly, Hydraulic Actuator Unit)
2. **Production Processing**: The order moves through 5 manufacturing stations:
   - CNC-Milling-01 (M-101)
   - Welding-Robot-02 (M-202)
   - Assembly-Cell-03 (M-303)
   - QC-Inspection-04 (M-404)
   - Packaging-05 (M-505)
3. **Order Completion**: Final status update when production finishes

### Generated Events

The seeder generates three types of events streamed to Fabric Eventstream:

| Event Type | Description | Frequency |
|------------|-------------|-----------|
| **Order Events** | Order creation and completion with customer details, product info, quantities, and delivery dates | Start & end of each order |
| **Machine Telemetry** | Real-time sensor data: temperature, vibration, cycle time, pressure, and operational status | Every 5 seconds |
| **Assembly Events** | Production milestones: steps started/completed, delays, and anomaly alerts | Every 5 seconds |
| **Production KPIs** | Overall Equipment Effectiveness (OEE), throughput, scrap rate, and uptime metrics | Every 15 seconds |

### Realistic Data Patterns

- **Gaussian distributions** for normal operational parameters (temperature ~72°C, vibration ~0.5g)
- **Anomaly injection** with 8% probability causing spikes in temperature (+25°C) and vibration (+1.5g)
- **Varied production conditions** across different stations and time periods
- **Unique identifiers** for orders, customers, and machines

## How the Seeder Works

```
┌─────────────────────────────────────────────────────────────────┐
│                     Smart Factory Simulator                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Parse command line args (number of parallel sessions)       │
│                          ↓                                       │
│  2. Initialize Event Hub Producer Client                        │
│                          ↓                                       │
│  3. Spawn N parallel factory sessions                           │
│                          ↓                                       │
│  ┌──────────────────────────────────────────────────┐          │
│  │         Per Session (RunFactorySessionAsync)      │          │
│  ├──────────────────────────────────────────────────┤          │
│  │  a. Generate unique order (ID, product, customer)│          │
│  │  b. Send Order Created event → Eventstream       │          │
│  │  c. Loop for 180 seconds:                        │          │
│  │     - Cycle through 5 stations                   │          │
│  │     - Generate machine telemetry                 │          │
│  │     - Generate assembly events                   │          │
│  │     - Generate KPIs (every 3rd cycle)            │          │
│  │     - Randomly inject anomalies (8% chance)      │          │
│  │     - Send batch → Eventstream                   │          │
│  │     - Wait 5 seconds                             │          │
│  │  d. Send Order Completed event → Eventstream     │          │
│  └──────────────────────────────────────────────────┘          │
│                          ↓                                       │
│  4. Wait for all sessions to complete                           │
│                          ↓                                       │
│  5. Exit                                                         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                    ┌─────────────────┐
                    │ Fabric Eventstream│
                    └─────────────────┘
                              ↓
            ┌─────────────────────────────────────┐
            │   Fabric Analytics & Processing     │
            │  • KQL Database                     │
            │  • Real-Time Analytics              │
            │  • Power BI Reports                 │
            │  • Lakehouse                        │
            │  • AI/ML Models                     │
            └─────────────────────────────────────┘
```

## Features

- **Parallel Execution**: Run multiple factory order sessions simultaneously to generate high-volume data
- **Command Line Control**: Specify the number of parallel sessions via command line argument
- **Session Identification**: Each session is labeled (e.g., `[Session 1]`, `[Session 2]`) for easy tracking
- **Independent Orders**: Each session creates its own order with unique ID and processes it independently
- **Event Hub Integration**: Direct streaming to Azure Event Hubs / Fabric Eventstream
- **JSON Serialization**: All events are serialized using modern .NET source generators for optimal performance

## Usage

### Method 1: Using the run script
```bash
./run.sh 5  # Run 5 parallel sessions
./run.sh    # Run 1 session (default)
```

### Method 2: Using dotnet directly

```bash
dotnet run          # Run 1 session (default)
dotnet run -- 3     # Run 3 parallel sessions
dotnet run -- 10    # Run 10 parallel sessions
```

### Method 3: Using the compiled executable

```bash
cd bin/Debug/net9.0
./SmartFactorySimulator    # Run 1 session (default)
./SmartFactorySimulator 5  # Run 5 parallel sessions
```

## Example Output

When running with 3 parallel sessions (`./run.sh 3`):

```
══════════════════════════════════════════════════════
  🏭 Smart Factory Simulator (C# → Fabric Eventstream)
══════════════════════════════════════════════════════
  Running 3 parallel session(s)
══════════════════════════════════════════════════════

[Session 1] 📦 Order created: ORD-A1B2C3D4 — Turbine Blade Assembly x5 for CUST-ALPHA
[Session 1]    Expected delivery: 2026-05-10
[Session 2] 📦 Order created: ORD-E5F6G7H8 — Hydraulic Actuator Unit x3 for CUST-BRAVO
[Session 2]    Expected delivery: 2026-04-25
[Session 3] 📦 Order created: ORD-I9J0K1L2 — Structural Fuselage Panel x7 for CUST-CHARLIE
[Session 3]    Expected delivery: 2026-05-01

[Session 1] 🏭 Production started for ORD-A1B2C3D4
[Session 2] 🏭 Production started for ORD-E5F6G7H8
[Session 3] 🏭 Production started for ORD-I9J0K1L2
...
```

## Parallel Session Architecture

The simulator supports running multiple independent factory sessions concurrently, enabling realistic high-volume data generation for stress testing and production-scale demos.

### How Parallel Execution Works

1. **Argument Parsing**: The program reads the first command line argument as the number of concurrent sessions to run
2. **Task Creation**: Creates `n` parallel tasks, each running an independent `RunFactorySessionAsync()` method
3. **Concurrent Execution**: All sessions run simultaneously using `Task.WhenAll()`, each with its own order lifecycle
4. **Thread Safety**: Each session has its own `Random` instance seeded with a unique GUID to prevent race conditions
5. **Shared Resources**: All sessions share the same Event Hub producer client but generate independent events with unique order IDs
6. **Synchronized Completion**: The program waits for all sessions to complete before exiting

### Data Volume Scaling

| Sessions | Events/Minute | Orders/Hour | Use Case |
|----------|---------------|-------------|----------|
| 1 | ~48 | 20 | Development, testing |
| 5 | ~240 | 100 | Small-scale demo |
| 10 | ~480 | 200 | Production simulation |
| 20+ | ~960+ | 400+ | Stress testing, high-volume scenarios |

## Configuration

All configuration is stored in `appsettings.json`, which is excluded from version control to protect your secrets.

### Initial Setup

1. **Copy the example configuration file:**
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. **Update `appsettings.json` with your credentials:**
   ```json
   {
     "EventHub": {
       "ConnectionString": "Endpoint=sb://YOUR-EVENTHUB.servicebus.windows.net/;SharedAccessKeyName=...",
       "EventHubName": "YOUR-ENTITY-PATH"
     },
     "Simulation": {
       "ProductionDurationSeconds": 180,
       "TelemetryIntervalMs": 5000,
       "AnomalyProbability": 0.08
     }
   }
   ```

### Getting Event Hub Credentials

**To get your connection string from Fabric Eventstream:**
1. Open your Fabric workspace
2. Navigate to your Eventstream
3. Open the "Custom App" source
4. Go to the "Keys" tab
5. Copy the connection string and entity path
6. Paste them into your `appsettings.json`

### Simulation Parameters

You can adjust the simulation behavior in `appsettings.json`:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `ProductionDurationSeconds` | Duration of each production run | 180 (3 minutes) |
| `TelemetryIntervalMs` | Telemetry emission frequency | 5000 (5 seconds) |
| `AnomalyProbability` | Chance of anomaly per cycle | 0.08 (8%) |

### Factory Configuration

The simulator uses predefined factory stations and product catalogs:

- **Stations**: 5 manufacturing stations (CNC Milling, Welding Robot, Assembly Cell, QC Inspection, Packaging)
- **Products**: 3 aerospace components (Turbine Blade Assembly, Hydraulic Actuator Unit, Structural Fuselage Panel)
- **Customers**: 3 customer identifiers (CUST-ALPHA, CUST-BRAVO, CUST-CHARLIE)

These can be customized in the arrays at the top of `Program.cs`.

## Prerequisites

- **.NET 9.0 SDK** or later
- **Azure Event Hubs** or **Microsoft Fabric Eventstream** with connection credentials
- **NuGet Packages** (automatically restored):
  - `Azure.Messaging.EventHubs`
  - `Microsoft.Extensions.Configuration`
  - `Microsoft.Extensions.Configuration.Json`

## Security

> **Important**: Never commit `appsettings.json` to version control. It contains your Event Hub connection strings and should remain local only. The `.gitignore` is configured to exclude it automatically.

## Building and Running

### First-time setup

```bash
# Navigate to the project directory
cd SmartFactorySimulator

# Copy the example configuration
cp appsettings.example.json appsettings.json

# Edit appsettings.json with your Event Hub credentials
# (Use your favorite editor: nano, vim, VS Code, etc.)
nano appsettings.json

# Restore dependencies and build
dotnet build

# Run with default settings (1 session)
dotnet run
```

### Production use

```bash
# Build in Release mode for optimal performance
dotnet build -c Release

# Run with multiple sessions for high-volume data generation
cd bin/Release/net9.0
./SmartFactorySimulator 10
```

## Integration with Microsoft Fabric

This seeder is designed to work seamlessly with Microsoft Fabric's real-time intelligence capabilities:

### Recommended Fabric Architecture

```
Smart Factory Simulator
         ↓
    Eventstream (Custom App Source)
         ↓
    ├─→ KQL Database (Real-time Analytics)
    ├─→ Lakehouse (Historical Storage)
    ├─→ Reflex (Alerting on Anomalies)
    └─→ Power BI (Real-time Dashboard)
```

### Sample Scenarios

1. **Real-Time Monitoring Dashboard**
   - Stream data to KQL Database
   - Create Power BI reports showing live factory status
   - Alert on anomaly detection (temperature spikes, vibration issues)

2. **Predictive Maintenance**
   - Store historical telemetry in Lakehouse
   - Train ML models on anomaly patterns
   - Deploy models for predictive alerts

3. **Order Tracking & Analytics**
   - Track order lifecycle from creation to completion
   - Analyze production efficiency metrics (OEE, throughput, scrap rate)
   - Calculate average cycle times per station

4. **Quality Control**
   - Monitor real-time quality metrics
   - Identify stations with high scrap rates
   - Correlate anomalies with quality issues

## Event Schema

### Order Event
```json
{
  "EventCategory": "Order",
  "Timestamp": "2026-04-14T10:30:00Z",
  "OrderId": "ORD-A1B2C3D4",
  "CustomerId": "CUST-ALPHA",
  "ProductName": "Turbine Blade Assembly",
  "Quantity": 5,
  "Status": "Received",
  "ExpectedDelivery": "2026-05-10T00:00:00Z"
}
```

### Machine Telemetry Event
```json
{
  "EventCategory": "MachineTelemetry",
  "Timestamp": "2026-04-14T10:30:05Z",
  "OrderId": "ORD-A1B2C3D4",
  "MachineId": "M-101",
  "StationName": "CNC-Milling-01",
  "Temperature": 72.5,
  "Vibration": 0.52,
  "CycleTimeSeconds": 45.3,
  "Pressure": 150.2,
  "Status": "Normal"
}
```

### Assembly Event
```json
{
  "EventCategory": "AssemblyEvent",
  "Timestamp": "2026-04-14T10:30:05Z",
  "OrderId": "ORD-A1B2C3D4",
  "StationName": "CNC-Milling-01",
  "EventType": "StepCompleted",
  "Details": "CNC-Milling-01 processing normally",
  "DurationSeconds": 45.3
}
```

### Production KPI Event
```json
{
  "EventCategory": "ProductionKPI",
  "Timestamp": "2026-04-14T10:30:05Z",
  "OrderId": "ORD-A1B2C3D4",
  "OEE": 0.85,
  "Throughput": 95,
  "ScrapRate": 0.02,
  "Uptime": 0.95
}
```

## Troubleshooting

### Configuration Issues

**Problem**: `InvalidOperationException: EventHub:ConnectionString not found in appsettings.json`  
**Solution**: 
- Ensure `appsettings.json` exists in the project directory
- Copy from example: `cp appsettings.example.json appsettings.json`
- Verify the JSON structure matches the example
- Make sure the file is being copied to the output directory (check the `.csproj`)

**Problem**: Application fails to start with no error message  
**Solution**: 
- Check that `appsettings.json` is valid JSON (no trailing commas, proper quotes)
- Verify all required configuration keys are present
- Run with `dotnet run` from the project directory (not from bin/)

### Connection Issues

**Problem**: Unable to connect to Event Hub  
**Solution**: 
- Verify connection string is correct and contains both endpoint and EntityPath
- Ensure your network allows outbound connections to Azure Event Hubs (port 5671)
- Check that the Event Hub exists and is in a running state

### Build Errors

**Problem**: `The type or namespace name 'Azure' could not be found`  
**Solution**: 
```bash
dotnet restore
dotnet build
```

### Performance Issues

**Problem**: High CPU usage with many parallel sessions  
**Solution**: 
- Reduce the number of parallel sessions
- Increase `TelemetryIntervalMs` to reduce event frequency
- Run in Release mode for better performance

### No Data Appearing in Fabric

**Problem**: Simulator runs but no data appears in Fabric  
**Solution**:
- Verify Eventstream is running and the Custom App source is configured
- Check Event Hub connection string matches your Eventstream credentials
- Look for error messages in the console output
- Verify the Eventstream has destination(s) configured

## Tips for Demo Success

1. **Start Small**: Test with 1-2 sessions before scaling up
2. **Pre-configure Fabric**: Set up Eventstream, KQL Database, and Power BI reports before running the seeder
3. **Use Anomalies**: The 8% anomaly rate creates interesting patterns for demos
4. **Monitor Performance**: Watch Event Hub metrics to ensure you're not hitting throttling limits
5. **Prepare Visuals**: Have Power BI dashboards ready to show real-time data flowing in