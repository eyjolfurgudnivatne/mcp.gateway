## Kartlegging av muligheter

### Dagens situasjon

```csharp
public sealed record AddNumbersRequest(
    [property: JsonPropertyName("number1")] double Number1,
    [property: JsonPropertyName("number2")] double Number2);

public sealed record AddNumbersResponse(
    [property: JsonPropertyName("result")] double Result);

[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""number1"":{""type"":""number"",""description"":""First number to add""},
            ""number2"":{""type"":""number"",""description"":""Second number to add""}
        },
        ""required"": [""number1"",""number2""]
    }")]
public JsonRpcMessage AddNumbersTool(JsonRpcMessage request)
{
    var args = request.GetParams<AddNumbersRequest>()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");

    return ToolResponse.Success(
        request.Id,
        new AddNumbersResponse(args.Number1 + args.Number2));
}
```

### Mål/muligheter for forbedring

```csharp
public sealed record AddNumbersRequestNy(
    [property: JsonPropertyName("number1")][Description("First number to add")] double Number1,
    [property: JsonPropertyName("number2")][Description("Second number to add")] double Number2);

public sealed record AddNumbersResponseNy(
    [property: JsonPropertyName("result")] double Result);


[McpTool("add_numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8")]
public JsonRpcMessage AddNumbersToolNy(TypedJsonRpc<AddNumbersRequestNy> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");

    return ToolResponse.Success(
        request.Id,
        new AddNumbersResponseNy(args.Number1 + args.Number2));
}
```

```csharp
TypedJsonRpc<TParams>

[property: JsonPropertyName("number1")][Description("First number to add")] double Number1,

- der feks: double er required og double? er optional

[property: JsonPropertyName("number1")][Description("First number to add")] enum Number1,
[property: JsonPropertyName("number1")][Description("First number to add")] string? Number1,

- oneOf, anyOf, if/then, enums osv. Usikker. Har ikke brukt dem ennå.
```

### Spørsmål til Copilot

- Er dette mulig å få til?
- InputSchema vinner alltid
- Krav: Full bakoverkompatibilitet

Diskuter gjerne i denne filen. Bruk Norsk.

---

## Vurderinger

#### TypedJsonRpc<T>

```csharp
public readonly struct TypedJsonRpc<T>
{
    private readonly JsonRpcMessage _inner;

    public TypedJsonRpc(JsonRpcMessage inner) => _inner = inner;

    public object? Id => _inner.Id;
    public string? IdAsString => _inner.IdAsString;
    public string? Method => _inner.Method;
    public JsonRpcMessage Inner => _inner;

    public T? GetParams() => _inner.GetParams<T>();

    // Evt. noen helper-metoder til
}
```

#### ToolInvoker

```csharp
// Pseudokode: når en tool-metode har TypedJsonRpc<T> som parameter
var inner = /* eksisterende JsonRpcMessage */;
var typed = new TypedJsonRpc<TParams>(inner);
invokeDelegate(typed /* + evt. andre DI-parametre */);
```

#### Start veldig smalt for auto‑schema:
-	Støtt bare enkle cases:
    -	primitive typer (string, double, int, bool)
    -	nullable vs non‑nullable → required‑liste
    -	enkle enums → type: "string" + enum: [...] (eller number hvis du vil)
    -	[Description] → Description
-	Ikke prøv å støtte oneOf/anyOf/if/then i v1.3. Det kan heller dokumenteres som “for avanserte cases, sett InputSchema manuelt”.

#### Ikke bland JSON‑schema‑generering inn i selve TypedJsonRpc
-	La TypedJsonRpc<T> være ren runtime‑kontrakt.
-	Ha en separat “schema generator” som tar typeof(T) og lager et (enkelt) schema når InputSchema == null.

#### JSON Schema typer – grunnmapping til C#

For en eventuell fremtidig schema-generator (v1.3+ eller v1.4+):

- `"type": "string"`
  - C#: `string`, `Guid` (`format: "uuid"`), `DateTime` / `DateTimeOffset` (`format: "date-time"`)
- `"type": "number"`
  - C#: `double`, `float`, `decimal`
- `"type": "integer"`
  - C#: `int`, `long`, `short`, osv.
- `"type": "boolean"`
  - C#: `bool`
- `"type": "array"`
  - C#: `T[]`, `List<T>`
- `"type": "object"`
  - C#: `record` / `class` (f.eks. `AddNumbersRequest`)
- `"type": ["string", "null"]`
  - C#: `string?` (nullable referansetype) eller `T?` for verdi-typer
- Enums:
  - C#: `enum Status { Active, Disabled }`
  - Schema (string-basert):

    ```json
    { "type": "string", "enum": ["Active", "Disabled"] }
    ```

    eller heltall-basert:

    ```json
    { "type": "integer", "enum": [0, 1] }
    ```

> Merk: `oneOf` / `anyOf` / `if` / `then` / `else` brukes når et felt kan ha flere **helt ulike** former (polymorfe objekter), ikke for enkle status-felt. For slike avanserte tilfeller skal vi i overskuelig fremtid fortsatt bruke eksplisitt `InputSchema` i stedet for auto-generering.

---

### Oppsummert anbefaling til plan.md
Hvis vi justerer planen for v1.3:
-	Ja, `TypedJsonRpc<TParams>` er mulig og gir mening som et tynt typed lag rundt `JsonRpcMessage` for verktøy.
-	`InputSchema` skal fortsatt vinne – i v1.3 rører vi ikke dagens oppførsel; eventuell auto-schema brukes kun når `InputSchema == null` (og selv det kan uansett utsettes til en senere versjon).
-	Full bakoverkompatibilitet beholdes ved:
    -	å ikke endre `JsonRpcMessage`-signaturen eller wire-formatet,
    -	å behandle `TypedJsonRpc<T>` som en ren hjelpestruktur i tool-metoder,
    -	å la eksisterende tools og schema fungere uendret.

**Konklusjon v1.3:**
-	Introduser `TypedJsonRpc<TParams>` som et tynt wrapper‑API rundt `JsonRpcMessage` for verktøy.
-	Ikke endre wire‑formatet eller `JsonRpcMessage`.
-	`InputSchema` forblir kilden til sannhet; eventuelt auto‑schema fra `TParams` vurderes først for en senere versjon og kun når `InputSchema == null`.

