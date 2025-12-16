# MCP Resources – Oversikt for v1.5.0

**Forfatter:** ARKo AS - AHelse Development Team  
**Dato:** 16. desember 2025  
**Status:** Research & Design  
**Versjon:** Draft 1.0

---

## 📋 Innhold

1. [Hva er MCP Resources?](#hva-er-mcp-resources)
2. [Resources vs Tools vs Prompts](#resources-vs-tools-vs-prompts)
3. [MCP Protocol – Resources API](#mcp-protocol--resources-api)
4. [Arkitektur for v1.5.0](#arkitektur-for-v150)
5. [Implementasjonsplan](#implementasjonsplan)
6. [Eksempler](#eksempler)
7. [Testing](#testing)
8. [Avgrensninger](#avgrensninger)

---

## 🎯 Hva er MCP Resources?

**MCP Resources** er den tredje hovedtypen i Model Context Protocol (ved siden av Tools og Prompts).

Resources representerer **data eller innhold** som serveren kan tilby til klienten:
- **Statisk innhold**: Filer, dokumenter, konfigurasjon
- **Dynamisk innhold**: Database-data, API-resultater, live-feed
- **Kontekstuell informasjon**: Metadata, status, systeminfo

### Nøkkelforskjeller

| Aspekt | Tools | Prompts | **Resources** |
|--------|-------|---------|---------------|
| **Formål** | Utfører handlinger | Tekstmaler | **Tilbyr data/innhold** |
| **Retning** | Client → Server → Result | Client ← Maler | **Client ← Data** |
| **Eksempel** | `add_numbers`, `send_email` | `summarize`, `translate` | **`file://logs/app.log`**, **`db://users/123`** |
| **MCP Metoder** | `tools/list`, `tools/call` | `prompts/list`, `prompts/get` | **`resources/list`**, **`resources/read`** |

**Nøkkelidé:** Resources gjør at serveren kan eksponere data som klienten (LLM) kan **lese og forstå** som kontekst.

---

## 🔌 MCP Protocol – Resources API

I følge MCP-spesifikasjonen (2025-06-18) består Resources-støtten av:

### 1️⃣ `resources/list` – Discovery

Lister alle tilgjengelige resources.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/list",
  "id": "res-list-1"
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "res-list-1",
  "result": {
    "resources": [
      {
        "uri": "file://logs/app.log",
        "name": "Application Logs",
        "description": "Server application logs",
        "mimeType": "text/plain"
      },
      {
        "uri": "db://users/123",
        "name": "User Profile",
        "description": "User profile data",
        "mimeType": "application/json"
      }
    ]
  }
}
```

### 2️⃣ `resources/read` – Hent innhold

Henter innholdet til en spesifikk resource.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/read",
  "id": "res-read-1",
  "params": {
    "uri": "file://logs/app.log"
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": "res-read-1",
  "result": {
    "contents": [
      {
        "uri": "file://logs/app.log",
        "mimeType": "text/plain",
        "text": "[2025-12-16 10:00:00] INFO: Application started\n[2025-12-16 10:01:00] DEBUG: Processing request..."
      }
    ]
  }
}
```

### 3️⃣ `resources/subscribe` (valgfri) – Live oppdateringer

For dynamiske resources som endrer seg over tid.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/subscribe",
  "id": "res-sub-1",
  "params": {
    "uri": "db://users/123"
  }
}
```

**Notifications (server → client):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "uri": "db://users/123"
  }
}
```

---

## 🏗️ Resources vs Tools vs Prompts

### Når bruker du hva?

**Tools** – når du vil **gjøre noe**:
```csharp
[McpTool("send_email")]
public JsonRpcMessage SendEmail(JsonRpcMessage request)
{
    // Action: Send e-post
    return ToolResponse.Success(request.Id, new { sent = true });
}
```

**Prompts** – når du vil gi **tekstmaler til LLM**:
```csharp
[McpPrompt(Description = "Summarize document")]
public JsonRpcMessage SummarizePrompt(JsonRpcMessage request)
{
    // Template: System + User messages
    return ToolResponse.Success(request.Id, new PromptResponse(...));
}
```

**Resources** – når du vil **tilby data/innhold**:
```csharp
[McpResource("file://logs/app.log", MimeType = "text/plain")]
public JsonRpcMessage AppLogs(JsonRpcMessage request)
{
    // Data: Returner loggfiler
    var logs = File.ReadAllText("app.log");
    return ToolResponse.Success(request.Id, new ResourceResponse(...));
}
```

### Bruksscenario for Resources

| Use Case | Eksempel | URI Pattern |
|----------|----------|-------------|
| **Filer** | Logs, dokumenter, bilder | `file://path/to/file` |
| **Database** | User profiles, produkter | `db://table/id` |
| **API** | Eksterne data-kilder | `http://api.example.com/data` |
| **System** | Status, metrics, config | `system://status`, `system://metrics` |
| **Dynamisk** | Real-time feed, notifikasjoner | `stream://notifications` |

---

## 🎯 Use Cases og Limitasjoner

### ✅ Støttet i v1.5.0

#### 1. Dynamiske fil-/mapplister (readonly)
**Perfect use case!** Resources kan brukes til å liste filer dynamisk.

**Eksempel:**
```csharp
[McpResource("file://project/files",
    Name = "Project Files",
    Description = "Dynamic list of all project files",
    MimeType = "application/json")]
public JsonRpcMessage ProjectFilesList(JsonRpcMessage request)
{
    // Scan files dynamisk ved hver lesing
    var files = Directory.GetFiles("./", "*.*", SearchOption.AllDirectories)
        .Select(f => new 
        { 
            path = f, 
            size = new FileInfo(f).Length,
            modified = new FileInfo(f).LastWriteTime 
        });
    
    var json = JsonSerializer.Serialize(new { files });
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "file://project/files",
            MimeType: "application/json",
            Text: json
        ));
}
```

**Use case:** GitHub Copilot kan spørre "hvilke filer har vi?" → Server scanner → Returnerer liste.

---

#### 2. Eksterne API-kall (med caching)
**Fungerer bra!** Resources kan hente data fra internett.

**Eksempel:**
```csharp
[McpResource("http://api.weather.com/current",
    Name = "Weather Data",
    Description = "Current weather information",
    MimeType = "application/json")]
public async Task<JsonRpcMessage> WeatherData(
    JsonRpcMessage request,
    IMemoryCache cache,
    HttpClient httpClient)
{
    // Check cache først (viktig!)
    var cacheKey = "weather_current";
    if (cache.TryGetValue(cacheKey, out string? cachedData))
    {
        return ToolResponse.Success(
            request.Id,
            new ResourceContent(
                Uri: "http://api.weather.com/current",
                MimeType: "application/json",
                Text: cachedData
            ));
    }
    
    // Fetch from API
    var response = await httpClient.GetAsync("https://api.weather.com/current");
    var content = await response.Content.ReadAsStringAsync();
    
    // Cache i 5 minutter
    cache.Set(cacheKey, content, TimeSpan.FromMinutes(5));
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "http://api.weather.com/current",
            MimeType: "application/json",
            Text: content
        ));
}
```

**Tips:**
- ✅ Alltid cache eksterne API-kall
- ✅ Sett fornuftige timeout (30s default)
- ✅ Håndter rate limiting
- ✅ Log feil (API kan være nede)

---

#### 3. Små til mellomstore tekstfiler (<10 MB)
**Fungerer OK** med begrensninger.

**Eksempel med limitering:**
```csharp
[McpResource("file://logs/app.log",
    Name = "Application Logs (Last 1000 lines)",
    Description = "Recent application logs",
    MimeType = "text/plain")]
public JsonRpcMessage AppLogsLimited(JsonRpcMessage request)
{
    // Les BARE siste 1000 linjer (ikke hele filen!)
    var allLines = File.ReadAllLines("app.log");
    var recentLines = allLines.TakeLast(1000);
    var content = string.Join("\n", recentLines);
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "file://logs/app.log",
            MimeType: "text/plain",
            Text: content
        ));
}
```

**Anbefalinger:**
- ✅ Begrens til siste N linjer/bytes
- ✅ Bruk streaming for store filer (v1.6+)
- ⚠️ Ikke les hele filen hvis >10 MB

---

#### 4. System status og metrics
**Perfekt use case!** Dynamisk data uten side effects.

**Eksempel:**
```csharp
[McpResource("system://status",
    Name = "System Status",
    Description = "Current system health metrics",
    MimeType = "application/json")]
public JsonRpcMessage SystemStatus(JsonRpcMessage request)
{
    var status = new
    {
        uptime = Environment.TickCount64,
        memoryUsed = GC.GetTotalMemory(false) / (1024 * 1024), // MB
        threadCount = ThreadPool.ThreadCount,
        cpuUsage = GetCpuUsage(), // Helper method
        diskSpace = GetDiskSpace(), // Helper method
        timestamp = DateTime.UtcNow
    };
    
    var json = JsonSerializer.Serialize(status, JsonOptions.Default);
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "system://status",
            MimeType: "application/json",
            Text: json
        ));
}
```

---

#### 5. Database queries
**Fungerer utmerket!** Hent data fra database.

**Eksempel:**
```csharp
[McpResource("db://products/featured",
    Name = "Featured Products",
    Description = "Top 10 featured products",
    MimeType = "application/json")]
public async Task<JsonRpcMessage> FeaturedProducts(
    JsonRpcMessage request,
    IProductService productService)
{
    var products = await productService.GetFeaturedAsync(limit: 10);
    var json = JsonSerializer.Serialize(products);
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "db://products/featured",
            MimeType: "application/json",
            Text: json
        ));
}
```

---

### ⚠️ Begrensninger i v1.5.0

#### 1. Store filer (>10 MB) må limiteres
**Problem:** `resources/read` returnerer alt innhold i én JSON-melding.

**Løsning for v1.5.0:**
```csharp
// Returner BARE relevant subset
var lastLines = File.ReadLines("huge.log").TakeLast(1000);
```

**Fremtidig løsning (v1.6+):**
- Chunked reading (paginering)
- Binary streaming via WebSocket
- Range requests (`bytes=0-1024`)

---

#### 2. Binary data kun som base64-encoded text
**Problem:** Ingen native blob support i v1.5.0.

**Workaround:**
```csharp
[McpResource("file://image.png",
    Name = "Logo Image",
    MimeType = "image/png")]
public JsonRpcMessage LogoImage(JsonRpcMessage request)
{
    var bytes = File.ReadAllBytes("logo.png");
    var base64 = Convert.ToBase64String(bytes);
    
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "file://image.png",
            MimeType: "image/png",
            Text: base64 // Base64-encoded
        ));
}
```

**Fremtidig løsning (v1.6+):**
- Native `Blob` field i `ResourceContent`
- Direct binary transfer

---

#### 3. Ingen streaming support
**Problem:** Én stor respons for hele innholdet.

**Impact:**
- Timeout for store filer (>30s)
- Høy memory usage
- Dårlig UX for slow data

**Fremtidig løsning (v1.6+):**
- Resource streaming (som `ToolConnector`)
- Progress callbacks
- Cancellation support

---

#### 4. Ingen real-time updates
**Problem:** `resources/read` er pull-based (ikke push).

**Workaround:**
```csharp
// Client må polle manuelt
while (true)
{
    var data = await ReadResource("system://status");
    await Task.Delay(5000); // Poll hver 5. sekund
}
```

**Fremtidig løsning (v1.6+):**
- `resources/subscribe` - Server pusher updates
- WebSocket notifications
- Event-driven architecture

---

### 🔜 Planlagt for v1.6+ / v2.0

#### 1. Resource streaming for store filer
```csharp
[McpResource("file://bigfile.zip", 
    Capabilities = ToolCapabilities.BinaryStreaming)]
public async Task BigFileStream(ToolConnector connector)
{
    using var file = File.OpenRead("bigfile.zip");
    using var stream = connector.OpenWrite(...);
    
    var buffer = new byte[64 * 1024]; // 64KB chunks
    int bytesRead;
    while ((bytesRead = await file.ReadAsync(buffer)) > 0)
    {
        await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
    }
    
    await stream.CompleteAsync();
}
```

---

#### 2. Native binary blob support
```csharp
public sealed record ResourceContent(
    string Uri,
    string? MimeType,
    string? Text = null,
    byte[]? Blob = null  // Native binary support!
);
```

---

#### 3. resources/subscribe for live updates
```csharp
// Server pusher automatisk når data endres
await client.SubscribeAsync("db://users/123");

# Server sender notification:
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": { "uri": "db://users/123" }
}
```

---

#### 4. Chunked/paginert file reading
```csharp
// Client spesifiserer range
await ReadResource("file://logs/app.log?offset=1000&limit=100");

// Server returnerer chunk
{
  "contents": [{
    "uri": "file://logs/app.log",
    "text": "Lines 1000-1100...",
    "range": { "start": 1000, "end": 1100 },
    "total": 5000
  }
```

---

### 📋 Oppsummering

| Scenario | v1.5.0 Support | Anbefaling |
|----------|----------------|------------|
| **Dynamisk fil-liste** | ✅ Full support | Implementer nå |
| **Eksterne API (cached)** | ✅ Full support | Implementer nå (husk cache!) |
| **Små tekstfiler (<10 MB)** | ⚠️ Fungerer OK | Limit til subset (siste N linjer) |
| **Store filer (>10 MB)** | ❌ Ikke anbefalt | Defer til v1.6+ (streaming) |
| **Binary filer (bilder, PDF)** | ⚠️ Base64 workaround | OK for små filer, defer store til v1.6+ |
| **System metrics** | ✅ Perfekt! | Implementer nå |
| **Database queries** | ✅ Full support | Implementer nå |
| **Real-time updates** | ❌ Må polle | Defer til v1.6+ (subscribe) |

---

## 🧱 Arkitektur for v1.5.0

### Parallelt mønster som Tools og Prompts

I v1.4.0 introduserte vi `[McpPrompt]` og `PromptService`. For v1.5.0 følger vi samme mønster:

#### 1. Nye modeller i `Mcp.Gateway.Tools`:

**ResourceModels.cs:**
```csharp
/// <summary>
/// Represents an MCP resource definition.
/// </summary>
public sealed record ResourceDefinition(
    string Uri,
    string Name,
    string? Description,
    string? MimeType);

/// <summary>
/// Represents the result of a resources/list call.
/// </summary>
public sealed record ResourceListResponse(
    IEnumerable<ResourceDefinition> Resources);

/// <summary>
/// Content of a resource (from resources/read).
/// </summary>
public sealed record ResourceContent(
    string Uri,
    string? MimeType,
    string? Text = null,
    byte[]? Blob = null);

/// <summary>
/// Response for resources/read.
/// </summary>
public sealed record ResourceReadResponse(
    IEnumerable<ResourceContent> Contents);
```

#### 2. Ny attributt: `McpResourceAttribute`

**McpResourceAttribute.cs:**
```csharp
/// <summary>
/// Marks a method as an MCP resource.
/// Resource URI must be specified.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpResourceAttribute : Attribute
{
    public McpResourceAttribute(string uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }

    /// <summary>
    /// Resource URI (e.g., "file://logs/app.log").
    /// Must follow URI format.
    /// </summary>
    public string Uri { get; }
    
    /// <summary>
    /// Human-readable name (optional).
    /// If null, derived from URI.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Resource description (optional).
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// MIME type (e.g., "text/plain", "application/json").
    /// Optional, can be auto-detected.
    /// </summary>
    public string? MimeType { get; set; }
}
```

#### 3. Utvid `ToolService` til å støtte Resources

**ToolService.cs** (eksisterende):
```csharp
public enum FunctionTypeEnum
{
    Tool,
    Prompt,
    Resource  // NY for v1.5.0
}
```

**Ny metode i ToolService:**
```csharp
/// <summary>
/// Gets all resource definitions.
/// </summary>
public IEnumerable<ResourceDefinition> GetAllResourceDefinitions()
{
    // Scan for [McpResource] attributes
    // Return ResourceDefinition for each
}

/// <summary>
/// Gets a specific resource by URI.
/// </summary>
public ResourceDefinition GetResourceDefinition(string uri)
{
    // Lookup by URI
}

/// <summary>
/// Invokes a resource method to read its content.
/// </summary>
public object InvokeResourceDelegate(string uri, params object[] args)
{
    // Similar to InvokeFunctionDelegate
}
```

#### 4. Utvid `ToolInvoker` med Resources-metoder

**ToolInvoker.cs** (eksisterende):
```csharp
// NY: Handle resources/list
if (message.Method == "resources/list")
{
    return HandleResourcesList(message);
}

// NY: Handle resources/read
if (message.Method == "resources/read")
{
    return await HandleResourcesReadAsync(message, cancellationToken);
}

// NY: Handle resources/subscribe (optional for v1.5.0)
if (message.Method == "resources/subscribe")
{
    return HandleResourcesSubscribe(message);
}
```

**Implementasjon:**
```csharp
/// <summary>
/// Handles MCP resources/list request.
/// </summary>
private JsonRpcMessage HandleResourcesList(JsonRpcMessage request)
{
    try
    {
        var resources = _toolService.GetAllResourceDefinitions();
        
        var resourcesList = resources.Select(r => new
        {
            uri = r.Uri,
            name = r.Name,
            description = r.Description,
            mimeType = r.MimeType
        }).ToList();

        return ToolResponse.Success(request.Id, new
        {
            resources = resourcesList
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in resources/list");
        return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
    }
}

/// <summary>
/// Handles MCP resources/read request.
/// </summary>
private async Task<JsonRpcMessage> HandleResourcesReadAsync(
    JsonRpcMessage request,
    CancellationToken cancellationToken)
{
    try
    {
        var requestParams = request.GetParams();
        var uri = requestParams.GetProperty("uri").GetString();
        
        if (string.IsNullOrEmpty(uri))
        {
            return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
        }

        // Build resource request
        var resourceRequest = JsonRpcMessage.CreateRequest("resource_read", request.Id, new { uri });

        // Invoke resource method
        var result = _toolService.InvokeResourceDelegate(uri, resourceRequest);

        // Process result (sync or async)
        var processedResult = await ProcessToolResultAsync(result, ..., cancellationToken);

        // Extract content
        object? resultData = null;
        if (processedResult is JsonRpcMessage msg)
        {
            resultData = msg.Result;
        }
        else
        {
            resultData = processedResult;
        }

        // Wrap in MCP resources/read format
        return ToolResponse.Success(request.Id, new
        {
            contents = new[]
            {
                new
                {
                    uri,
                    mimeType = "text/plain",  // Or from ResourceDefinition
                    text = resultData
                }
            }
        });
    }
    catch (ToolNotFoundException ex)
    {
        return ToolResponse.Error(request.Id, -32601, "Resource not found", new { detail = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in resources/read");
        return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
    }
}
```

#### 5. Oppdater `initialize` for å inkludere Resources capability

**ToolInvoker.HandleInitialize():**
```csharp
private JsonRpcMessage HandleInitialize(JsonRpcMessage request)
{
    bool hasTools = _toolService.GetAllFunctionDefinitions(FunctionTypeEnum.Tool).Any();
    bool hasPrompts = _toolService.GetAllFunctionDefinitions(FunctionTypeEnum.Prompt).Any();
    bool hasResources = _toolService.GetAllResourceDefinitions().Any();  // NY

    Dictionary<string, object> capabilities = [];

    if (hasTools) capabilities["tools"] = new { };
    if (hasPrompts) capabilities["prompts"] = new { };
    if (hasResources) capabilities["resources"] = new { };  // NY

    return ToolResponse.Success(request.Id, new
    {
        protocolVersion = "2025-06-18",
        serverInfo = new { name = "mcp-gateway", version = "2.0.0" },
        capabilities
    });
}
```

---

## 💻 Implementasjonsplan

### Phase 1: Modeller og attributt (Dag 1)

**Tasks:**
- [ ] Opprett `ResourceModels.cs` med:
  - `ResourceDefinition`
  - `ResourceListResponse`
  - `ResourceContent`
  - `ResourceReadResponse`
- [ ] Opprett `McpResourceAttribute.cs`
- [ ] Utvid `ToolService.FunctionTypeEnum` med `Resource`
- [ ] Kompiler og verifiser ingen breaking changes

**Expected outcome:**
- Nye typer kompilerer ✅
- Eksisterende kode uendret

---

### Phase 2: ToolService discovery (Dag 2-3)

**Tasks:**
- [ ] Utvid `ToolService` til å scanne etter `[McpResource]`
- [ ] Implementer `GetAllResourceDefinitions()`
- [ ] Implementer `GetResourceDefinition(string uri)`
- [ ] Implementer `InvokeResourceDelegate(string uri, params object[] args)`
- [ ] Valider URI format (må følge `scheme://path` pattern)

**Expected outcome:**
- Resources kan registreres via attributt
- ToolService finner alle resources
- URI validation fungerer

---

### Phase 3: ToolInvoker MCP-metoder (Dag 4-5)

**Tasks:**
- [ ] Implementer `HandleResourcesList()`
- [ ] Implementer `HandleResourcesReadAsync()`
- [ ] Oppdater `HandleInitialize()` med resources capability
- [ ] Håndter feil (resource not found, invalid URI, etc.)

**Expected outcome:**
- `resources/list` returnerer alle resources
- `resources/read` henter innhold
- `initialize` viser `resources` capability

---

### Phase 4: Eksempel og testing (Dag 6-7)

**Tasks:**
- [ ] Opprett `Examples/ResourceMcpServer`:
  - Simple file resource (`file://logs/app.log`)
  - Simple data resource (`db://users/123`)
  - System status resource (`system://status`)
- [ ] Opprett `Examples/ResourceMcpServerTests`:
  - `ResourceListTests` – verifiser `resources/list`
  - `ResourceReadTests` – verifiser `resources/read`
  - `McpProtocolTests` – verifiser `initialize` med resources
- [ ] Oppdater `DevTestServer` med eksempel-resources
- [ ] Oppdater `Mcp.Gateway.Tests` med resources-tester

**Expected outcome:**
- Minst 3 eksempel-resources fungerer
- Alle tester passerer (HTTP/WS/stdio)
- `initialize` viser `resources` capability

---

### Phase 5: Dokumentasjon (Dag 8)

**Tasks:**
- [ ] Oppdater `docs/MCP-Protocol.md` med Resources-seksjon
- [ ] Oppdater `docs/MCP-Protocol-Verification.md`
- [ ] Oppdater `Mcp.Gateway.Tools/README.md` med Resources-eksempler
- [ ] Oppdater `CHANGELOG.md` for v1.5.0
- [ ] Oppdater `CONTRIBUTING.md` (hvis nødvendig)

**Expected outcome:**
- Komplett dokumentasjon for Resources
- Klare eksempler på bruk
- API-referanse oppdatert

---

## 📝 Eksempler

### Eksempel 1: Enkel fil-resource

**DevTestServer/Tools/Resources/FileResource.cs:**
```csharp
using Mcp.Gateway.Tools;

public class FileResource
{
    [McpResource("file://logs/app.log",
        Name = "Application Logs",
        Description = "Server application logs",
        MimeType = "text/plain")]
    public JsonRpcMessage AppLogs(JsonRpcMessage request)
    {
        var logs = File.ReadAllText("app.log");
        
        return ToolResponse.Success(
            request.Id,
            new ResourceContent(
                Uri: "file://logs/app.log",
                MimeType: "text/plain",
                Text: logs
            ));
    }
}
```

### Eksempel 2: Database-resource med DI

**DevTestServer/Tools/Resources/UserResource.cs:**
```csharp
using Mcp.Gateway.Tools;

public class UserResource
{
    [McpResource("db://users/{id}",
        Name = "User Profile",
        Description = "User profile data",
        MimeType = "application/json")]
    public async Task<JsonRpcMessage> UserProfile(
        JsonRpcMessage request,
        UserService userService)
    {
        var requestParams = request.GetParams();
        var uri = requestParams.GetProperty("uri").GetString();
        
        // Extract {id} from URI
        var id = ExtractIdFromUri(uri);
        
        // Fetch user from database
        var user = await userService.GetUserAsync(id);
        
        var json = JsonSerializer.Serialize(user);
        
        return ToolResponse.Success(
            request.Id,
            new ResourceContent(
                Uri: uri!,
                MimeType: "application/json",
                Text: json
            ));
    }
    
    private static int ExtractIdFromUri(string? uri)
    {
        // Simple extraction: db://users/123 → 123
        var parts = uri?.Split('/');
        return int.Parse(parts?.LastOrDefault() ?? "0");
    }
}
```

### Eksempel 3: System status resource

**DevTestServer/Tools/Resources/SystemResource.cs:**
```csharp
using Mcp.Gateway.Tools;

public class SystemResource
{
    [McpResource("system://status",
        Name = "System Status",
        Description = "Current system status and metrics",
        MimeType = "application/json")]
    public JsonRpcMessage SystemStatus(JsonRpcMessage request)
    {
        var status = new
        {
            uptime = Environment.TickCount64,
            memoryUsed = GC.GetTotalMemory(false),
            threadCount = ThreadPool.ThreadCount,
            timestamp = DateTime.UtcNow
        };
        
        var json = JsonSerializer.Serialize(status);
        
        return ToolResponse.Success(
            request.Id,
            new ResourceContent(
                Uri: "system://status",
                MimeType: "application/json",
                Text: json
            ));
    }
}
```

---

## 🧪 Testing

### Test-struktur (parallelt med Tools og Prompts)

**Examples/ResourceMcpServerTests:**
```
ResourceMcpServerTests/
├── Fixture/
│   └── ResourceMcpServerFixture.cs
├── Resources/
│   ├── ResourceListTests.cs
│   ├── ResourceReadTests.cs
│   └── McpProtocolTests.cs
```

### Test 1: `resources/list`

```csharp
[Fact]
public async Task ResourcesList_ReturnsAllResources()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "resources/list",
        id = "res-list-1"
    };

    // Act
    var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);
    response.EnsureSuccessStatusCode();
    
    var content = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(content);
    
    // Assert
    Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
    Assert.True(result.TryGetProperty("resources", out var resourcesElement));
    
    var resources = resourcesElement.EnumerateArray().ToList();
    Assert.True(resources.Count >= 1, "Expected at least 1 resource");
    
    // Verify file://logs/app.log exists
    var appLog = resources.FirstOrDefault(r => r.GetProperty("uri").GetString() == "file://logs/app.log");
    Assert.True(appLog.ValueKind != JsonValueKind.Undefined, "App log resource not found");
    Assert.Equal("Application Logs", appLog.GetProperty("name").GetString());
    Assert.Equal("text/plain", appLog.GetProperty("mimeType").GetString());
}
```

### Test 2: `resources/read`

```csharp
[Fact]
public async Task ResourcesRead_ReturnsContent()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "resources/read",
        id = "res-read-1",
        @params = new
        {
            uri = "file://logs/app.log"
        }
    };

    // Act
    var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);
    response.EnsureSuccessStatusCode();
    
    var content = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(content);
    
    // Assert
    Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
    Assert.True(result.TryGetProperty("contents", out var contentsElement));
    
    var contents = contentsElement.EnumerateArray().ToList();
    Assert.Single(contents);
    
    var firstContent = contents[0];
    Assert.Equal("file://logs/app.log", firstContent.GetProperty("uri").GetString());
    Assert.Equal("text/plain", firstContent.GetProperty("mimeType").GetString());
    Assert.True(firstContent.TryGetProperty("text", out var text));
    Assert.NotEmpty(text.GetString());
}
```

### Test 3: `initialize` med resources

```csharp
[Fact]
public async Task Initialize_IncludesResourcesCapability()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "initialize",
        id = "init-1"
    };

    // Act
    var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);
    response.EnsureSuccessStatusCode();
    
    var content = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(content);
    
    // Assert
    Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
    Assert.True(result.TryGetProperty("capabilities", out var capabilities));
    
    // Should have tools, prompts, AND resources
    Assert.True(capabilities.TryGetProperty("tools", out _));
    Assert.True(capabilities.TryGetProperty("prompts", out _));
    Assert.True(capabilities.TryGetProperty("resources", out _));  // NY
}
```

---

## ⚠️ Avgrensninger for v1.5.0

### ✅ Inkludert i v1.5.0

- `[McpResource]` attributt
- `ResourceDefinition`, `ResourceContent`, `ResourceReadResponse` modeller
- `resources/list` – discovery
- `resources/read` – hent innhold
- `initialize` – resources capability
- Grunnleggende URI validation
- Static og dynamic resources (både sync og async metoder)
- MIME type support (text/plain, application/json)
- Dependency injection support for resource methods
- Eksempler: file, database, system resources
- Komplett test coverage (HTTP/WS/stdio)

### 🔜 Utsatt til v1.6+ / v2.0

- `resources/subscribe` / `resources/unsubscribe` – live updates
- `resources/templates` – URI templates med variabel-substitusjon
- `resources/update` – skriv/oppdater resources (CRUD)
- Avansert caching av resource content
- Resource permissions / access control
- Binary blob support (bilder, PDF, etc.) – Kun text-based i v1.5.0
- Resource metadata (size, modified date, etc.)

---

## 📚 Referanser

### MCP Specification

- **Official Spec:** https://spec.modelcontextprotocol.io/specification/2025-06-18/
- **Resources Section:** https://spec.modelcontextprotocol.io/specification/2025-06-18/basic/resources
- **MCP GitHub:** https://github.com/modelcontextprotocol/specification

### Microsoft Docs

- **API Management MCP Support:** https://learn.microsoft.com/en-us/azure/api-management/mcp-server-overview
- **Azure MCP Server:** https://github.com/Azure/azure-mcp

### Eksisterende dokumentasjon

- `docs/MCP-Protocol.md` – MCP implementation overview
- `docs/MCP-Protocol-Verification.md` – Compliance verification
- `.internal/notes/v1.4.0/plan-v1.4.0.md` – Prompts implementation (parallelt mønster)

---

## 🎯 Suksesskriterier for v1.5.0

- ✅ `[McpResource]` attributt fungerer
- ✅ `resources/list` returnerer alle registrerte resources
- ✅ `resources/read` henter innhold (text-based)
- ✅ `initialize` viser `resources` capability
- ✅ Minst 3 eksempel-resources (file, db, system)
- ✅ Alle tester passerer (70+ tester totalt)
- ✅ Zero breaking changes fra v1.4.0
- ✅ Komplett dokumentasjon
- ✅ GitHub Copilot kan lese resources

---

**Status:** Klar for implementering 🚀  
**Neste steg:** Start Phase 1 (Modeller og attributt)
