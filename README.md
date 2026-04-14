# Fabric Demo Seeder

A realistic data generator for Microsoft Fabric demonstrations that simulates a smart manufacturing environment with streaming telemetry data.

## What is This?

This repository contains a **Smart Factory Simulator** that generates realistic IoT and manufacturing data for Microsoft Fabric demos. It simulates orders flowing through a virtual factory production line, complete with machine telemetry, assembly events, and production KPIs.

## Project Structure

```
fabric-demo-seeder/
├── SmartFactorySimulator/        # Main simulator application
│   ├── Program.cs                # Core simulation logic
│   ├── appsettings.example.json  # Configuration template
│   ├── appsettings.json          # Your local config (gitignored)
│   ├── run.sh                    # Convenience script for running
│   └── README.md                 # Detailed documentation
└── .gitignore                    # Excludes secrets and build artifacts
```

## Quick Start

### 1. Clone and Navigate

```bash
git clone <your-repo-url>
cd fabric-demo-seeder/SmartFactorySimulator
```

### 2. Configure Event Hub

```bash
# Copy the example configuration
cp appsettings.example.json appsettings.json

# Edit with your Fabric Eventstream credentials
nano appsettings.json
```

### 3. Build and Run

```bash
# Build the project
dotnet build

# Run with 1 session (default)
dotnet run

# Or run with multiple parallel sessions
dotnet run -- 5
```

## What Gets Generated

The simulator produces four types of events streamed to your Fabric Eventstream:

- **Order Events** - Order creation and completion
- **Machine Telemetry** - Temperature, vibration, cycle time, pressure (every 5s)
- **Assembly Events** - Production milestones and anomaly alerts (every 5s)
- **Production KPIs** - OEE, throughput, scrap rate, uptime (every 15s)

## Use Cases

Perfect for demonstrating:
- Real-time streaming analytics in Fabric
- KQL Database and real-time dashboards
- Lakehouse data ingestion and transformation
- Predictive maintenance ML models
- Power BI real-time reports
- Alerting and automation with Reflex

## Configuration

All secrets are stored in `appsettings.json` (not tracked in git). See `appsettings.example.json` for the template.

Required configuration:
- Event Hub connection string from Fabric Eventstream
- Event Hub name / entity path
- Simulation parameters (duration, frequency, anomaly rate)

## Documentation

For detailed documentation, configuration options, troubleshooting, and integration guides, see:

**[📖 SmartFactorySimulator/README.md](SmartFactorySimulator/README.md)**

## Requirements

- .NET 9.0 SDK or later
- Microsoft Fabric workspace with Eventstream
- Azure Event Hubs connection credentials

## Security

⚠️ **Important**: Never commit `appsettings.json` to version control. It contains your Event Hub connection strings and should remain local only. The `.gitignore` is configured to exclude it automatically.

## License

This project is licensed under the MIT License. View [LICENSE](LICENSE) for details. 
