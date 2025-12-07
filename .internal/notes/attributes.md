## Tool Attributes

Dagens løsning:

```csharp
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number"",""description"":""First number to add""},
                ""number2"":{""type"":""number"",""description"":""Second number to add""}
            },
            ""required"":[""number1"",""number2""]
        }")]

    public async Task<JsonRpcMessage> AddNumbersTool(JsonRpcMessage request)
    {
        var args = request.GetParams<NumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new NumbersResponse(args.Number1 + args.Number2));
    }
```

En mulig fremtidig hybrid løsning:
```csharp
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8")]
    [McpToolProperty("number1", Type = "number", Description = "First number to add", Required = true)]
    [McpToolProperty("number2", Type = "number", Description = "Second number to add", Required = true)]
    public async Task<JsonRpcMessage> AddNumbersTool(JsonRpcMessage request)
    {
        var args = request.GetParams<NumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");
        return ToolResponse.Success(
            request.Id,
            new NumbersResponse(args.Number1 + args.Number2));
    }
```
Se `Mcp.Gateway.Tools.McpToolAttribute` og `Mcp.Gateway.Tools.McpToolPropertyAttribute`

---

## 🤔 Filosofisk Analyse

### Spørsmål 1: Hvor mange forskjellige typer har en property?

**JSON Schema types (basis):**
1. `"string"` - Tekst
2. `"number"` - Alle tall (int, float, double)
3. `"integer"` - Kun heltall
4. `"boolean"` - true/false
5. `"array"` - Lister
6. `"object"` - Nestede objekter
7. `"null"` - null-verdier

**JSON Schema formats (for "string"):**
- `"date-time"` - ISO 8601 datetime
- `"email"` - Email adresse
- `"uri"` - URL/URI
- `"uuid"` - GUID
- `"pattern"` - Regex pattern

**Enum:**
- Defineres via `"enum": ["value1", "value2"]`
- Kan brukes med string, number, etc.

**Konklusjon:** 
- **7 base types** + **formats** + **constraints** (min/max, pattern, etc.)
- Relativt håndterbart, men kompleksitet vokser raskt!

---

### Spørsmål 2: Blir dette for komplisert?

**🔴 MINE BEKYMRINGER:**

#### Problem 1: JSON Schema er komplisert!
```csharp
// Enkel property:
[McpToolProperty("email", Type = "string", Format = "email")]

// Array property:
[McpToolProperty("tags", Type = "array", ItemType = "string")]

// Nested object:
[McpToolProperty("user", Type = "object")]  // Men hva med struktur?

// Enum:
[McpToolProperty("status", Type = "string", Enum = new[] {"active", "inactive"})]

// Conditional (hvis dette, så det):
// ??? Umulig med attributes!
```

**Dette blir fort VELDIG komplisert!**

#### Problem 2: Dobbeltvedlikehold
```csharp
// Du må FORTSATT ha record:
record NumbersRequest(double Number1, double Number2);

// OG attributes:
[McpToolProperty("number1", Type = "number")]
[McpToolProperty("number2", Type = "number")]

// To steder å vedlikeholde samme informasjon!
```

#### Problem 3: Manglende fleksibilitet
```csharp
// JSON Schema kan ha:
{
  "oneOf": [...],
  "anyOf": [...],
  "allOf": [...],
  "if": {..., "then": {...}}
}

// Dette er UMULIG å uttrykke med attributes!
```

---

### 🟢 ALTERNATIV: Hybrid Tool API (bedre!)

**I stedet for `McpToolProperty`, bruk method parameters!**

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result")]
public double AddNumbers(
    [Description("First number to add")] double number1,
    [Description("Second number to add")] double number2
)
{
    return number1 + number2;
}
```

**Auto-generates InputSchema:**
```json
{
  "type": "object",
  "properties": {
    "number1": {"type": "number", "description": "First number to add"},
    "number2": {"type": "number", "description": "Second number to add"}
  },
  "required": ["number1", "number2"]
}
```

**Fordeler:**
1. ✅ **Type-safety** - C# compiler validerer typer
2. ✅ **Enkelhet** - Én kilde til sannhet (method parameters)
3. ✅ **Lesbarhet** - Kode er enklere å lese
4. ✅ **Ingen dobbeltvedlikehold** - Schema genereres automatisk

**Ulemper:**
1. ❌ **Komplekse schemas** - Må fortsatt bruke InputSchema for advanced JSON Schema
2. ❌ **Reflection/Source Generators** - Krever runtime eller compile-time code gen

---

### Spørsmål 3: Default to `"type":"object"`?

**JA!** ✅

**Hvorfor?**
- JSON-RPC 2.0 `params` er alltid et object
- MCP Protocol bruker alltid object for `arguments`
- Jeg har aldri sett andre typer i praksis

**Bevis fra MCP spec:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "add_numbers",
    "arguments": {          // ← Alltid object!
      "number1": 5,
      "number2": 3
    }
  }
}
```

**Konklusjon:** Hardcode `"type": "object"` - ingen grunn til fleksibilitet her.

---

### Spørsmål 4: McpTool.Name nullable + auto-generert?

**🟢 ABSOLUTT JA!** Dette er **genial** idé!

```csharp
// Option 1: Explicit name
[McpTool("add_numbers")]
public double AddTool(...) { }

// Option 2: Auto-generated from method name
[McpTool]  // ← Name is null
public double AddNumbersTool(...) { }
// Auto-generates: "add_numbers_tool"
```

**Implementation:**
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    // Primary constructor with optional name
    public McpToolAttribute(string? name = null)
    {
        Name = name;
    }
    
    public string? Name { get; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? InputSchema { get; set; }
}
```

**Tool discovery logic:**
```csharp
var attr = method.GetCustomAttribute<McpToolAttribute>();
var toolName = attr.Name ?? ConvertToSnakeCase(method.Name);

// Validate
if (!ToolMethodNameValidator.IsValid(toolName, out var error))
    throw new InvalidOperationException($"Invalid tool name '{toolName}': {error}");
```

**Helper method:**
```csharp
private static string ConvertToSnakeCase(string methodName)
{
    // "AddNumbersTool" → "add_numbers_tool"
    // "GetUserById" → "get_user_by_id"
    
    return Regex.Replace(methodName, "(?<!^)([A-Z])", "_$1")
        .ToLowerInvariant();
}
```

---

## 🎯 MIN ANBEFALING

**IKKE bruk `McpToolPropertyAttribute`!**

**Bruk i stedet:**

### 1️⃣ For enkle tools: Hybrid Tool API (v1.1+)

```csharp
[McpTool]  // Name auto-generated: "add_numbers"
/// <summary>Adds two numbers</summary>
public double AddNumbers(double a, double b)
{
    return a + b;
}
```

**Benefits:**
- ✅ Type-safe
- ✅ Clean og lesbar
- ✅ Auto-generated schema
- ✅ Ingen dobbeltvedlikehold

---

### 2️⃣ For komplekse tools: Explicit InputSchema (current)

```csharp
[McpTool("complex_query",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""filters"":{""type"":""array"",""items"":{...}},
            ""options"":{""type"":""object"",""properties"":{...}}
        }
    }")]
public JsonRpcMessage ComplexQuery(JsonRpcMessage request)
{
    // Full control for complex scenarios
}
```

**Benefits:**
- ✅ Full kontroll
- ✅ Støtter alle JSON Schema features
- ✅ Ingen begrensninger

---

## 📊 Oppsummering: Attributes vs. Hybrid API

| Aspekt | McpToolPropertyAttribute | Hybrid Tool API |
|--------|-------------------------|-----------------|
| **Enkelhet** | ⚠️ Middels (mange attributes) | ✅ Høy (naturlig C#) |
| **Type-safety** | ❌ Nei (strenger i attributes) | ✅ Ja (compiler validation) |
| **Lesbarhet** | ⚠️ Middels (attributt-spam) | ✅ Høy (clean method signature) |
| **Fleksibilitet** | ❌ Begrenset (kan ikke uttrykke alt) | ✅ Høy (fallback til InputSchema) |
| **Vedlikehold** | ❌ Dobbelt (attributes + record) | ✅ Single source of truth |
| **Kompleksitet** | 🔴 Høy (JSON Schema er komplisert) | 🟢 Lav (C# types → JSON Schema) |

---

## 💡 KONKLUSJON

**Mitt råd:**

1. ✅ **Implementer Hybrid Tool API** (see HybridToolAPI-Plan.md) - **DEFERRED to v2.0+**
2. ✅ **Gjør McpTool.Name nullable** + auto-generate from method name - **✅ DONE in v1.1**
3. ❌ **IKKE implementer McpToolPropertyAttribute** (for komplisert, lite verdi) - **✅ REMOVED**
4. ✅ **Behold InputSchema** for komplekse schemas (fallback) - **✅ KEPT**

**Hvorfor?**
- **Hybrid Tool API** gir deg 80% av fordelene uten kompleksiteten - **BUT v2.0+ due to complexity**
- **Method parameters** er type-safe og naturlig C# - **Deferred: XML docs parsing too complex**
- **McpToolPropertyAttribute** blir for komplisert og gir lite ekstra verdi - **Removed from codebase**
- **InputSchema** dekker de siste 20% (komplekse scenarios) - **Current solution, works well**
- **Auto-naming** løser 80% av use cases uten kompleksitet - **✅ Implemented in v1.1**

---

## 📋 BESLUTNING (5. desember 2025)

**v1.1 Scope:**
- ✅ Auto-generated tool names (DONE)
- ❌ Hybrid Tool API (deferred to v2.0+)
- ❌ XML documentation parsing (too complex, not worth it)

**Begrunnelse:**
1. **XML docs parsing** er for komplisert:
   - Krever separate XML-filer
   - Reflection kan ikke lese XML comments direkte
   - Må parse XML-filer runtime
   - **Løsning:** Bruk `Description` property direkte i `[McpTool]`

2. **Hybrid Tool API** er for ambisiøst for v1.1:
   - Request ID context management (AsyncLocal)
   - Return type wrapping
   - Parameter resolution ambiguity
   - Schema generation complexity
   - **Løsning:** Defer til v2.0 med bedre design

3. **Current solution fungerer utmerket:**
   - Explicit names eller auto-naming
   - Description i attribute
   - InputSchema for full kontroll
   - Type-safe med records
   - **Enkelt og kraftig!**

**Fremtiden (v2.0+):**
- Hvis vi vil ha Hybrid API, må det designes grundig
- Start med simple types only
- Bruk standard .NET attributes ([Range], [EmailAddress], etc.)
- AsyncLocal for request context
- Clear separation: Opt-in, ikke replacement



