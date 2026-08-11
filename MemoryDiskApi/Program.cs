using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();


// ============================================================
// STORAGE
// ============================================================

// RAM storage
var memoryData = new List<MyData>();

// Disk storage
var filePath = Path.Combine(
    AppContext.BaseDirectory,
    "data.json"
);


// ============================================================
// 0. POST /generate
// Generate test data
// ============================================================

app.MapPost("/generate", (int count = 200) =>
{
    var stopwatch = Stopwatch.StartNew();

    var data = new List<MyData>();

    for (int i = 1; i <= count; i++)
    {
        data.Add(new MyData
        {
            Id = i,
            Name = $"User-{i:000}",
            Value = $"This is test data for record number {i}. " +
                    $"This data is being used to compare RAM and disk performance."
        });
    }

    stopwatch.Stop();

    return Results.Ok(new
    {
        operation = "GENERATE",
        recordsGenerated = data.Count,
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
        data = data
    });
});


// ============================================================
// 1. POST /memory
// Save ONE record to RAM
// ============================================================

app.MapPost("/memory", (MyData data) =>
{
    var stopwatch = Stopwatch.StartNew();

    memoryData.Add(data);

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "RAM",
        operation = "POST",
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
        id = data.Id,
        totalItems = memoryData.Count
    });
});


// ============================================================
// 2. GET /memory
// Get ALL records from RAM
// ============================================================

app.MapGet("/memory", () =>
{
    var stopwatch = Stopwatch.StartNew();

    var result = memoryData;

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "RAM",
        operation = "GET",
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
        totalItems = result.Count,
        data = result
    });
});


// ============================================================
// 3. POST /file
// Save ONE record to disk
// ============================================================

app.MapPost("/file", async (MyData data) =>
{
    var stopwatch = Stopwatch.StartNew();

    var json = JsonSerializer.Serialize(data);

    await File.AppendAllTextAsync(
        filePath,
        json + Environment.NewLine
    );

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "DISK",
        operation = "POST",
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
        id = data.Id
    });
});


// ============================================================
// 4. GET /file
// Get ALL records from disk
// ============================================================

app.MapGet("/file", async () =>
{
    var stopwatch = Stopwatch.StartNew();

    if (!File.Exists(filePath))
    {
        stopwatch.Stop();

        return Results.Ok(new
        {
            storage = "DISK",
            operation = "GET",
            elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
            totalItems = 0,
            data = Array.Empty<MyData>()
        });
    }

    var lines = await File.ReadAllLinesAsync(filePath);

    var result = lines
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => JsonSerializer.Deserialize<MyData>(x))
        .Where(x => x != null)
        .ToList();

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "DISK",
        operation = "GET",
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds,
        totalItems = result.Count,
        data = result
    });
});


// ============================================================
// 5. POST /memory/batch
// Generate + save 200 records directly to RAM
// ============================================================

app.MapPost("/memory/batch", (int count = 200) =>
{
    var stopwatch = Stopwatch.StartNew();

    for (int i = 1; i <= count; i++)
    {
        memoryData.Add(new MyData
        {
            Id = i,
            Name = $"User-{i:000}",
            Value = $"This is test data for record number {i}. " +
                    $"This data is being used to compare RAM and disk performance."
        });
    }

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "RAM",
        operation = "BATCH POST",
        recordsAdded = count,
        totalItems = memoryData.Count,
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds
    });
});


// ============================================================
// 6. POST /file/batch
// Generate + save 200 records directly to disk
// ============================================================

app.MapPost("/file/batch", async (int count = 200) =>
{
    var stopwatch = Stopwatch.StartNew();

    await using var stream = new FileStream(
        filePath,
        FileMode.Append,
        FileAccess.Write,
        FileShare.Read,
        bufferSize: 4096,
        useAsync: true
    );

    await using var writer = new StreamWriter(stream);

    for (int i = 1; i <= count; i++)
    {
        var data = new MyData
        {
            Id = i,
            Name = $"User-{i:000}",
            Value = $"This is test data for record number {i}. " +
                    $"This data is being used to compare RAM and disk performance."
        };

        var json = JsonSerializer.Serialize(data);

        await writer.WriteLineAsync(json);
    }

    await writer.FlushAsync();

    stopwatch.Stop();

    return Results.Ok(new
    {
        storage = "DISK",
        operation = "BATCH POST",
        recordsAdded = count,
        elapsedMicroseconds = stopwatch.Elapsed.TotalMicroseconds
    });
});


app.Run();


// ============================================================
// MODEL
// ============================================================

public class MyData
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Value { get; set; } = "";
}