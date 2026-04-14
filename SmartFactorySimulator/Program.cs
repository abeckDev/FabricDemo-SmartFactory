using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;

// ── Load Configuration ─────────────────────────────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

// ── Configuration ──────────────────────────────────────────────────────────
var eventHubConnectionString = configuration["EventHub:ConnectionString"] 
    ?? throw new InvalidOperationException("EventHub:ConnectionString not found in appsettings.json");
var eventHubName = configuration["EventHub:EventHubName"] 
    ?? throw new InvalidOperationException("EventHub:EventHubName not found in appsettings.json");

// ── Simulation parameters ──────────────────────────────────────────────────
var productionDurationSeconds = int.Parse(configuration["Simulation:ProductionDurationSeconds"] ?? "180");
var telemetryIntervalMs = int.Parse(configuration["Simulation:TelemetryIntervalMs"] ?? "5000");
var anomalyProbability = double.Parse(configuration["Simulation:AnomalyProbability"] ?? "0.08");

// ── Factory layout ─────────────────────────────────────────────────────────
var stations = new (string Name, string MachineId)[]
{
    ("CNC-Milling-01",    "M-101"),
    ("Welding-Robot-02",  "M-202"),
    ("Assembly-Cell-03",  "M-303"),
    ("QC-Inspection-04",  "M-404"),
    ("Packaging-05",      "M-505"),
};

var products = new[]
{
    "Turbine Blade Assembly",
    "Hydraulic Actuator Unit",
    "Structural Fuselage Panel",
};

var customers = new[] { "CUST-ALPHA", "CUST-BRAVO", "CUST-CHARLIE" };

// ── Parse command line arguments ───────────────────────────────────────────
var parallelSessions = 1; // Default to 1 session
if (args.Length > 0 && int.TryParse(args[0], out var n) && n > 0)
{
    parallelSessions = n;
}

// ── Create the EventHub producer client ────────────────────────────────────
await using var producer = new EventHubProducerClient(eventHubConnectionString, eventHubName);

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  🏭 Smart Factory Simulator (C# → Fabric Eventstream)");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine($"  Running {parallelSessions} parallel session(s)");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine();

// ── Run multiple parallel sessions ─────────────────────────────────────────
var tasks = new List<Task>();
for (int i = 0; i < parallelSessions; i++)
{
    var sessionId = i + 1;
    tasks.Add(RunFactorySessionAsync(
        producer, 
        sessionId, 
        stations, 
        products, 
        customers,
        productionDurationSeconds,
        telemetryIntervalMs,
        anomalyProbability));
}

await Task.WhenAll(tasks);

Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("✅ All sessions completed!");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();

// ═══════════════════════════════════════════════════════════════════════════
// Factory Session - One complete order lifecycle
// ═══════════════════════════════════════════════════════════════════════════
static async Task RunFactorySessionAsync(
    EventHubProducerClient producer,
    int sessionId,
    (string Name, string MachineId)[] stations,
    string[] products,
    string[] customers,
    int productionDurationSeconds,
    int telemetryIntervalMs,
    double anomalyProbability)
{
    var random = new Random(Guid.NewGuid().GetHashCode()); // Thread-safe random
    var sessionPrefix = $"[Session {sessionId}]";

    // ═══════════════════════════════════════════════════════════════════════════
    // Scene 1–2: Customer places an order → Virtual factory spins up
    // ═══════════════════════════════════════════════════════════════════════════
    var orderId = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    var product = products[random.Next(products.Length)];
    var quantity = random.Next(1, 11);
    var customerId = customers[random.Next(customers.Length)];
    var expectedDelivery = DateTime.UtcNow.AddDays(random.Next(5, 31));

    var order = new FactoryEvent
    {
        EventCategory = "Order",
        Timestamp = DateTime.UtcNow,
        OrderId = orderId,
        CustomerId = customerId,
        ProductName = product,
        Quantity = quantity,
        Status = "Received",
        ExpectedDelivery = expectedDelivery,
    };

    await SendEventAsync(producer, order);
    Console.WriteLine($"{sessionPrefix} 📦 Order created: {orderId} — {product} x{quantity} for {customerId}");
    Console.WriteLine($"{sessionPrefix}    Expected delivery: {expectedDelivery:yyyy-MM-dd}");
    Console.WriteLine();

    await Task.Delay(2000);

    // ═══════════════════════════════════════════════════════════════════════════
    // Scene 3–5: Production runs — telemetry, assembly events, KPIs
    // ═══════════════════════════════════════════════════════════════════════════
    Console.WriteLine($"{sessionPrefix} 🏭 Production started for {orderId}");
    Console.WriteLine($"{sessionPrefix} {new string('─', 60)}");

    var startTime = DateTime.UtcNow;
    var stepIndex = 0;

    while ((DateTime.UtcNow - startTime).TotalSeconds < productionDurationSeconds)
    {
        var station = stations[stepIndex % stations.Length];
        var now = DateTime.UtcNow;
        var isAnomaly = random.NextDouble() < anomalyProbability;

        // ── Machine Telemetry ──────────────────────────────────────────────
        var temperature = Math.Round(NextGaussian(random, 72, 3) + (isAnomaly ? 25 : 0), 1);
        var vibration = Math.Round(NextGaussian(random, 0.5, 0.1) + (isAnomaly ? 1.5 : 0), 2);
        var cycleTime = Math.Round(NextGaussian(random, 45, 5) + (isAnomaly ? 20 : 0), 1);
        var pressure = Math.Round(NextGaussian(random, 150, 10), 1);

        var telemetry = new FactoryEvent
        {
            EventCategory = "MachineTelemetry",
            Timestamp = now,
            OrderId = orderId,
            MachineId = station.MachineId,
            StationName = station.Name,
            Temperature = temperature,
            Vibration = vibration,
            CycleTimeSeconds = cycleTime,
            Pressure = pressure,
            Status = isAnomaly ? "Warning" : "Normal",
        };

        // ── Assembly Event ─────────────────────────────────────────────────
        var eventTypes = new[] { "StepCompleted", "StepStarted", "MilestoneReached" };
        var eventType = isAnomaly ? "Delay" : eventTypes[random.Next(eventTypes.Length)];

        var assemblyEvent = new FactoryEvent
        {
            EventCategory = "AssemblyEvent",
            Timestamp = now,
            OrderId = orderId,
            StationName = station.Name,
            EventType = eventType,
            Details = isAnomaly
                ? "⚠️ Anomaly detected — high vibration & temperature"
                : $"{station.Name} processing normally",
            DurationSeconds = Math.Round(NextGaussian(random, 45, 10), 1),
        };

        // Build batch
        var batch = await producer.CreateBatchAsync();
        AddToBatch(batch, telemetry);
        AddToBatch(batch, assemblyEvent);

        // ── Production KPIs (every 3rd cycle) ──────────────────────────────
        if (stepIndex % 3 == 0)
        {
            var kpi = new FactoryEvent
            {
                EventCategory = "ProductionKPI",
                Timestamp = now,
                OrderId = orderId,
                OEE = Math.Round(NextGaussian(random, 0.85, 0.05) - (isAnomaly ? 0.15 : 0), 2),
                Throughput = random.Next(80, 121),
                ScrapRate = Math.Round(NextGaussian(random, 0.02, 0.01) + (isAnomaly ? 0.05 : 0), 3),
                Uptime = Math.Round(NextGaussian(random, 0.95, 0.02), 2),
            };
            AddToBatch(batch, kpi);
        }

        await producer.SendAsync(batch);

        // Console output
        var statusIcon = isAnomaly ? "⚠️  ANOMALY" : "✅ Normal";
        Console.WriteLine($"{sessionPrefix}   [{station.Name,-20}] {statusIcon} | Temp: {temperature,6}°C | Vibration: {vibration,5}g | Cycle: {cycleTime,5}s");

        stepIndex++;
        await Task.Delay(telemetryIntervalMs);
    }

    // ── Order completed ────────────────────────────────────────────────────────
    var completionEvent = new FactoryEvent
    {
        EventCategory = "Order",
        Timestamp = DateTime.UtcNow,
        OrderId = orderId,
        CustomerId = customerId,
        ProductName = product,
        Quantity = quantity,
        Status = "Completed",
        ExpectedDelivery = expectedDelivery,
    };
    await SendEventAsync(producer, completionEvent);

    Console.WriteLine($"{sessionPrefix} {new string('─', 60)}");
    Console.WriteLine($"{sessionPrefix} ✅ Production complete for {orderId}");
    Console.WriteLine();
}

// ═══════════════════════════════════════════════════════════════════════════
// Helper methods
// ═══════════════════════════════════════════════════════════════════════════

static async Task SendEventAsync(EventHubProducerClient producer, FactoryEvent evt)
{
    using var batch = await producer.CreateBatchAsync();
    AddToBatch(batch, evt);
    await producer.SendAsync(batch);
}

static void AddToBatch(EventDataBatch batch, FactoryEvent evt)
{
    var json = JsonSerializer.Serialize(evt, JsonContext.Default.FactoryEvent);
    var eventData = new EventData(Encoding.UTF8.GetBytes(json));
    eventData.Properties["EventCategory"] = evt.EventCategory;

    if (!batch.TryAdd(eventData))
    {
        Console.WriteLine($"⚠️ Event too large for batch: {evt.EventCategory}");
    }
}

static double NextGaussian(Random rng, double mean, double stddev)
{
    var u1 = 1.0 - rng.NextDouble();
    var u2 = 1.0 - rng.NextDouble();
    var normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    return mean + stddev * normal;
}

// ═══════════════════════════════════════════════════════════════════════════
// Event model — always serialize all fields (no conditional ignoring)
// ═══════════════════════════════════════════════════════════════════════════

[JsonSerializable(typeof(FactoryEvent))]
internal partial class JsonContext : JsonSerializerContext { }

public class FactoryEvent
{
    public string EventCategory { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string OrderId { get; set; } = "";
    public string StationName { get; set; } = "";
    public string Status { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public string MachineId { get; set; } = "";
    public double Temperature { get; set; }
    public double Vibration { get; set; }
    public double CycleTimeSeconds { get; set; }
    public double Pressure { get; set; }
    public string EventType { get; set; } = "";
    public string Details { get; set; } = "";
    public double DurationSeconds { get; set; }
    public double OEE { get; set; }
    public int Throughput { get; set; }
    public double ScrapRate { get; set; }
    public double Uptime { get; set; }
}
