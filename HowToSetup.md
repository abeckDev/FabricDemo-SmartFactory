---

## Smart Factory Demo — Complete Setup Guide

---

## Step 1: Create Fabric Workspace & Eventhouse

1. Go to [app.fabric.microsoft.com](https://app.fabric.microsoft.com)
2. Create a new **Workspace** (e.g., `SmartFactoryDemo`)
3. Inside the workspace → **New** → **Eventhouse** (e.g., `FactoryEventhouse`)
4. This auto-creates a KQL Database inside it

---

## Step 2: Create All Tables

Open a **KQL Queryset** in your Eventhouse and run (all) these:
>Note: In the Fabric Query Editor, only the marked query will be run by default. You might need to click at and execute each query on its own. 

```kql name=01_create_tables.kql
// ── Landing table for all events from the simulator ──
.create table RawEvents (
    EventCategory: string,
    Timestamp: datetime,
    OrderId: string,
    StationName: string,
    Status: string,
    CustomerId: string,
    ProductName: string,
    Quantity: int,
    ExpectedDelivery: datetime,
    MachineId: string,
    Temperature: real,
    Vibration: real,
    CycleTimeSeconds: real,
    Pressure: real,
    EventType: string,
    Details: string,
    DurationSeconds: real,
    OEE: real,
    Throughput: int,
    ScrapRate: real,
    Uptime: real
)

// ── Typed destination tables ──
.create table Orders (
    OrderId: string,
    CustomerId: string,
    ProductName: string,
    Quantity: int,
    OrderTimestamp: datetime,
    Status: string,
    ExpectedDelivery: datetime
)

.create table MachineTelemetry (
    Timestamp: datetime,
    OrderId: string,
    MachineId: string,
    StationName: string,
    Temperature: real,
    Vibration: real,
    CycleTimeSeconds: real,
    Pressure: real,
    Status: string
)

.create table AssemblyEvents (
    Timestamp: datetime,
    OrderId: string,
    StationName: string,
    EventType: string,
    Details: string,
    DurationSeconds: real
)

.create table ProductionKPIs (
    Timestamp: datetime,
    OrderId: string,
    OEE: real,
    Throughput: int,
    ScrapRate: real,
    Uptime: real
)

// ── Supply risk reference table (for stretch scene) ──
.create table SupplyRisk (
    MaterialId: string,
    MaterialName: string,
    SupplierId: string,
    SupplierName: string,
    Region: string,
    RiskType: string,
    RiskScore: real,
    LastUpdated: datetime
)
```

---

## Step 3: Enable Streaming Ingestion

```kql name=02_enable_streaming.kql
.alter database ['FactoryEventhouse'] policy streamingingestion enable
```

---

## Step 4: Create JSON Ingestion Mapping for RawEvents

```kql name=03_create_mapping.kql
.create table RawEvents ingestion json mapping 'RawEventsMapping'
'['
'  {"column":"EventCategory","path":"$.EventCategory"},'
'  {"column":"Timestamp","path":"$.Timestamp"},'
'  {"column":"OrderId","path":"$.OrderId"},'
'  {"column":"StationName","path":"$.StationName"},'
'  {"column":"Status","path":"$.Status"},'
'  {"column":"CustomerId","path":"$.CustomerId"},'
'  {"column":"ProductName","path":"$.ProductName"},'
'  {"column":"Quantity","path":"$.Quantity"},'
'  {"column":"ExpectedDelivery","path":"$.ExpectedDelivery"},'
'  {"column":"MachineId","path":"$.MachineId"},'
'  {"column":"Temperature","path":"$.Temperature"},'
'  {"column":"Vibration","path":"$.Vibration"},'
'  {"column":"CycleTimeSeconds","path":"$.CycleTimeSeconds"},'
'  {"column":"Pressure","path":"$.Pressure"},'
'  {"column":"EventType","path":"$.EventType"},'
'  {"column":"Details","path":"$.Details"},'
'  {"column":"DurationSeconds","path":"$.DurationSeconds"},'
'  {"column":"OEE","path":"$.OEE"},'
'  {"column":"Throughput","path":"$.Throughput"},'
'  {"column":"ScrapRate","path":"$.ScrapRate"},'
'  {"column":"Uptime","path":"$.Uptime"}'
']'
```

---

## Step 5: Create Update Policies (Auto-Routing)
>Note: This can also be handled by using Filtering in the Stream processing. For  quick start purposes this way is a lot faster to setup though. 

These automatically split incoming `RawEvents` rows into the correct typed tables:

```kql name=04_update_policies.kql
// Route Order events
.alter table Orders policy update
@'[{"IsEnabled": true, "Source": "RawEvents", "Query": "RawEvents | where EventCategory == \"Order\" | project OrderId, CustomerId, ProductName, Quantity, OrderTimestamp=Timestamp, Status, ExpectedDelivery", "IsTransactional": false}]'

// Route Machine Telemetry events
.alter table MachineTelemetry policy update
@'[{"IsEnabled": true, "Source": "RawEvents", "Query": "RawEvents | where EventCategory == \"MachineTelemetry\" | project Timestamp, OrderId, MachineId, StationName, Temperature, Vibration, CycleTimeSeconds, Pressure, Status", "IsTransactional": false}]'

// Route Assembly events
.alter table AssemblyEvents policy update
@'[{"IsEnabled": true, "Source": "RawEvents", "Query": "RawEvents | where EventCategory == \"AssemblyEvent\" | project Timestamp, OrderId, StationName, EventType, Details, DurationSeconds", "IsTransactional": false}]'

// Route Production KPI events
.alter table ProductionKPIs policy update
@'[{"IsEnabled": true, "Source": "RawEvents", "Query": "RawEvents | where EventCategory == \"ProductionKPI\" | project Timestamp, OrderId, OEE, Throughput, ScrapRate, Uptime", "IsTransactional": false}]'
```

---

## Step 6: Seed Supply Risk Data

```kql name=05_seed_supply_risk.kql
.ingest inline into table SupplyRisk <|
MAT-001,Titanium Alloy Ti-6Al-4V,SUP-ALPHA,Nordic Metals AB,Scandinavia,Tariff,0.7,2026-04-14T08:00:00Z
MAT-002,Carbon Fiber Composite,SUP-BRAVO,EuroCarbon GmbH,Central Europe,Shortage,0.9,2026-04-14T08:00:00Z
MAT-003,Hydraulic Fluid MIL-H-5606,SUP-CHARLIE,PetroLub Inc,North America,Logistics,0.4,2026-04-14T08:00:00Z
MAT-004,Avionics Wire Harness,SUP-DELTA,Shenzhen ElectroParts,East Asia,Geopolitical,0.85,2026-04-14T08:00:00Z
MAT-005,Structural Steel S355,SUP-ECHO,Baltic Steel OÜ,Baltics,Weather,0.3,2026-04-14T08:00:00Z
```

---

## Step 7: Create Eventstream

1. In your workspace → **New** → **Eventstream** (e.g., `IngestionEventstream`)
2. **Add source** → **Custom App** → name it `FactorySimulatorInput` → **Publish**
3. Click the Custom App source node → copy the **Connection string** and **Event Hub name** from the Keys tab
4. **Add destination** → **Eventhouse** → select your `FactoryEventhouse` KQL Database → select the **`RawEvents`** table → select the **`RawEventsMapping`** JSON mapping
5. **Publish**

> In case the Event Stream should handle even category seperation, you would need to use Filter and Manage Fields transformations here for each category. It means better visuals but requires a little more time to setup. 

---

## Step 8: Build, Configure & Run the C# Simulator

Follow the steps in the [Smart Factory Simulator Documentation](SmartFactorySimulator/README.md). 

Build, Configure and run the project at least once to get sample data sent to your Fabric instance. 

## Step 9: Verify Data Is Flowing

After running the simulator for a bit, run these in your KQL Queryset:

```kql name=06_verify_data.kql
// Check raw landing table
RawEvents | count

// Check each typed table (populated by update policies)
Orders | take 5

MachineTelemetry | take 5

AssemblyEvents | take 5

ProductionKPIs | take 5

// Check supply risk seed data
SupplyRisk | take 5
```

---

## Step 10: Real-Time Dashboard

1. In your workspace → **New** → **Real-Time Dashboard**
2. Add your KQL Database as a data source
3. Add tiles:

```kql name=tile_live_machines.kql
// Live machine status -> Use Multi Stat Visual Type (Conditional Color formatting helps as well)
MachineTelemetry
| where Timestamp > ago(30m)
| summarize arg_max(Timestamp, *) by MachineId
| project MachineId, StationName, Temperature, Vibration, CycleTimeSeconds, Status
| order by MachineId asc
```

```kql name=tile_anomaly_timeline.kql
// Anomaly timeline -> Use Anomaly Chart Visual
MachineTelemetry
| where Timestamp > ago(1h)
| where Status == "Warning"
| summarize AnomalyCount = count() by bin(Timestamp, 1m), StationName
| render timechart
```

```kql name=tile_oee_trend.kql
// OEE trend -> Use Time Chart visual 
ProductionKPIs
| where Timestamp > ago(1h)
| summarize AvgOEE = avg(OEE) by bin(Timestamp, 2m)
| render timechart
```

```kql name=tile_supply_risk.kql
// Supply risk exposure for active orders -> Use Colum Chart visual
Orders
| where Status != "Completed"
| extend MaterialId = case(
    ProductName has "Turbine", "MAT-001",
    ProductName has "Hydraulic", "MAT-003",
    "MAT-002"
  )
| join kind=inner SupplyRisk on MaterialId
| where RiskScore > 0.6
| project OrderId, ProductName, SupplierName, Region, RiskType, RiskScore
| order by RiskScore desc
```

```kql name=EventCounter.kql
// Events processed in the last hour -> use Stat visual 
RawEvents
| where Timestamp > ago(1h)
| summarize
    TotalEvents = count(),
    Orders = countif(EventCategory == "Order"),
    TelemetryReadings = countif(EventCategory == "MachineTelemetry"),
    Anomalies = countif(EventCategory == "MachineTelemetry" and Status == "Warning")
```

```kql name=AnomalyCounter.kql
// Anomalies in the last 30 minutes -> use Stat visual (Conditional Color formatting helps as well)
MachineTelemetry
| where Timestamp > ago(5m)
| where Status == "Warning"
| summarize AnomalyCount = count()
```

---

## Step 11: Power BI Report

1. In Power BI Desktop or Fabric → **New Report**
2. **Get Data** → **Kusto (Azure Data Explorer)** → enter your Eventhouse URI (Can be obtained from the Eventhouse overview)

3. Build visuals:

### 1. OEE Gauge

1. Click on an empty area of the canvas
2. In the **Visualizations** pane → select **Gauge** (speedometer icon)
3. From the **Data** pane → expand **ProductionKPIs**
4. Drag **OEE** to the **Value** field
5. Click the dropdown on OEE in the Value field → change from "Sum" to **"Average"**
6. Set formatting:
   - **Minimum:** `0`
   - **Maximum:** `1`
   - **Target:** `0.85` (this adds a target line — great for storytelling: *"Our target OEE is 85%"*)
7. Title: **"Overall Equipment Effectiveness (OEE)"**

---

### 2. Throughput by Station — Bar Chart

1. Click empty canvas area
2. Select **Clustered Bar Chart** (horizontal bars)
3. From **MachineTelemetry**:
   - Drag **StationName** → **Y-axis**
   - Drag **CycleTimeSeconds** → **X-axis**
4. Click dropdown on CycleTimeSeconds → change to **"Average"**
5. Optional: Sort by average cycle time descending (click `...` on the visual → **Sort axis** → **Avg of CycleTimeSeconds** → **Descending**)
6. Title: **"Avg Cycle Time by Station"**

> **Tip:** If you'd rather show throughput counts, swap CycleTimeSeconds for any field and use **"Count"** — but avg cycle time per station tells a better performance story.

---

### 3. Scrap Rate Trend — Line Chart

1. Click empty canvas area
2. Select **Line Chart**
3. From **ProductionKPIs**:
   - Drag **Timestamp** → **X-axis**
   - Drag **ScrapRate** → **Y-axis**
4. Click dropdown on ScrapRate → change to **"Average"**
5. The X-axis may auto-group into a date hierarchy (Year > Quarter > Month). To fix:
   - Click the dropdown on **Timestamp** in the X-axis field → select **"Timestamp"** (not the hierarchy) to show raw time
   - Or right-click the axis → **"Don't summarize"**
6. Title: **"Scrap Rate Trend"**

---

### 4. Assembly Progress — Funnel

1. Click empty canvas area
2. Select **Funnel** visualization
3. From **AssemblyEvents**:
   - Drag **StationName** → **Category**
   - Drag **EventType** → **Values** (it will auto-count)
4. Or for a more meaningful funnel:
   - Drag **EventType** → **Category**
   - Drag **OrderId** → **Values** → set to **"Count"**
5. This shows how many events at each stage: `StepStarted` → `StepCompleted` → `MilestoneReached` — naturally forming a funnel shape
6. Title: **"Assembly Event Breakdown"**

> **Note:** A true manufacturing funnel would show orders flowing through stations in sequence. If the funnel doesn't look right, swap to a **Stacked Bar Chart** with StationName on the axis and EventType as the legend — this is often more readable.

---

### 5. Root Cause Slicer — Filters

Add **two slicer visuals** so the audience (or agent) can filter the entire report:

#### Slicer 1: Station Filter
1. Click empty canvas area
2. Select **Slicer** (filter icon)
3. From **MachineTelemetry** → drag **StationName** → **Field**
4. In the slicer formatting → set style to **"Dropdown"** or **"List"** (dropdown saves space)

#### Slicer 2: Event Type Filter
1. Click empty canvas area
2. Select **Slicer**
3. From **AssemblyEvents** → drag **EventType** → **Field**
4. Set style to **"Dropdown"**


---

## Step 12: Agent Builder

1. In your workspace → **New** → **Agent**
2. **Add data source** → select your KQL Database
3. Add instructions:

```text name=agent_instructions.txt
You are a Smart Factory Control Tower assistant. You help users understand
production status, order progress, factory performance, and supply chain risks.

When asked about an order, query the Orders table for status and the
AssemblyEvents table for progress details.

When asked about factory performance, query ProductionKPIs for OEE,
throughput, and scrap metrics.

When anomalies are detected, check MachineTelemetry for stations with
Status = 'Warning' and correlate with AssemblyEvents.

When asked about supply risks, join Orders with SupplyRisk on MaterialId.
Use this mapping:
- Products containing "Turbine" → MAT-001
- Products containing "Hydraulic" → MAT-003
- All other products → MAT-002

Flag any material with RiskScore > 0.6 as high risk.
Recommend alternative sourcing or expedited procurement for high-risk items.

Always be specific with numbers and timestamps.
```

4. Test queries:
   - *"What is the current status of order ORD-XXXXX?"*
   - *"Are there any anomalies on the shop floor?"*
   - *"What's our current OEE?"*
   - *"Are any active orders exposed to supply chain risk?"*

---

## Step 13 (Optional): Data Activator Alert

1. On your Real-Time Dashboard → anomaly tile → click **"Set Alert"**
2. **Object:** `MachineId`
3. **Property:** `Vibration`
4. **Condition:** `Vibration > 1.2 for longer than 60 seconds`
5. **Action:** Send email or Teams message
6. Save & activate

---

## Demo Day Checklist

| # | Task | When |
|---|---|---|
| 1 | Pre-run simulator for 10–15 min to seed dashboards with data | 30 min before |
| 2 | Have terminal ready with `dotnet run` command | At demo start |

---