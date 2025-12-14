## MCP Prompts support (official MCP spec feature)

### Kort oversikt

**MCP Prompts** er en egen ressurs‑type i MCP‑spesifikasjonen (på linje med tools og resources). Der tools beskriver
"funksjoner" som kan kalles, beskriver prompts **ferdige prompt‑maler** som klienten kan hente, vise og bruke direkte
eller med små justeringer.

På et høyt nivå gir Prompts:
- En liste over navngitte prompt‑maler (f.eks. `summarize`, `translate_to_norwegian`, `generate_release_notes`)
- For hver prompt:
  - En menneskevennlig `description`
  - Et sett parametre / placeholders (navn + beskrivelse + type)
  - En eller flere tekstblokker (ofte system+user) som inneholder selve malen
- Klienten (Copilot/Claude) kan:
  - Spørre serveren om hvilke prompts som finnes
  - Velge en prompt, fylle inn parametre, og sende den videre til LLM

### MCP Prompts vs Tools

**Likheter:**
- Begge er deklarative beskrivelser serveren tilbyr til klienten.
- Begge har navn, description og et sett parametre.
- Begge dukker opp i MCP discover/list‑kall (egen seksjon for prompts).

**Forskjeller:**
- Tools → beskriver **handlinger** på serversiden (JSON‑RPC kall, kode kjøres hos oss).
- Prompts → beskriver **tekstmaler** som klienten (LLM) bruker til å formulere nye meldinger.
- Tools returnerer JSON‑resultat; Prompts returnerer tekstblokker som klienten kan bruke direkte i en chat.

### Typisk MCP API for Prompts (konseptuelt)

> Navnene kan variere noe mellom tidlige utgaver av spesifikasjonen, men mønsteret er typisk slik:

- `prompts/list` – returnerer alle tilgjengelige prompts
  - Request: ingen params
  - Response: `{ prompts: [ { name, description, arguments: [...] } ] }`
- `prompts/get` – returnerer én prompt med utfylt mal
  - Request: `{ name, arguments }`
  - Response: `{ messages: [ { role, content }, ... ] }`

Her:
- `arguments` er et objekt med verdier for placeholders i prompt‑malen.
- `messages` er en liste med MCP/LLM‑meldinger (typisk system + user) som klienten så kan sende videre til LLM.

### Hvordan dette kan passe inn i MCP Gateway

For v1.4.0 ser det naturlig ut å:

1. **Legge til et enkelt prompt‑modellsett** i `Mcp.Gateway.Tools` (internt):
   - `PromptDefinition` (name, description, parameters, messages)
   - `PromptMessage` (role, content)
   - `PromptParameter` (name, description, type, required)

2. **Introdusere egne services per MCP‑konsept (skalerbart design):**
   - Beholde `ToolService` for tools (`[McpTool]` + `tools/*`).
   - Legge til `PromptService` for prompts (`[McpPrompt]` + `prompts/*`).
   - Senere kunne legge til `ResourceService` for resources (`[McpResource]` + `resources/*`).
   - Felles mønster:
     - Lazy assembly‑scan én gang.
     - Eget internt register per type (`ConfiguredTools`, `ConfiguredPrompts`, `ConfiguredResources`).

3. **Introdusere `[McpPrompt]` for attributt‑basert prompt‑registrering:**
   - Ny attributt, parallell til `[McpTool]`:
     - `McpPromptAttribute(string? name = null)` med `Name` (nullable) og `Description`.
   - Navn kan auto-genereres fra metodenavn (samme snake_case‑logikk som tools).
   - `PromptService` scanner assemblies etter `[McpPrompt]` og bygger opp en egen prompt‑liste (separat fra tools) for
     bruk i `prompts/list` og `prompts/get`.

4. **Utvid `ToolInvoker` til å håndtere MCP prompt‑metoder:**
   - Injiser både `ToolService` og `PromptService` (og senere evt. `ResourceService`) via DI.
   - `prompts/list` → henter alle `PromptDefinition` fra `PromptService` og returnerer dem i MCP-format.
   - `prompts/get` → henter en named prompt, fyller inn parametre (enkel placeholder‑erstatning i v1.4.0), og returnerer
     meldingslisten (`messages: [ { role, content }, ... ]`).

5. **Legge til ett eller to konkrete prompts i et eksempelprosjekt**, f.eks. i `Examples/CalculatorMcpServer`:
   - `summarize_calculation` – prompt som forklarer et regnestykke for sluttbruker.
   - `generate_release_notes` – prompt som tar inn en changelog‑bit og lager kort tekst.

6. **Tester:**
   - Nye tester i `Mcp.Gateway.Tests` for `prompts/list` og `prompts/get` (HTTP/WS/stdio etter behov).
   - Egen testklasse per metode, f.eks. `PromptsListTests`, `PromptsGetTests`, med fokus på:
     - At alle `[McpPrompt]` dukker opp i `prompts/list` med korrekt navn/description/arguments.
     - At `prompts/get` returnerer forventede `messages` når arguments fylles inn.

### Avgrensning for v1.4.0

Siden det ikke er tidspress på v1.4.0 kan vi ta inn en relativt komplett førsteversjon av Prompts, men med tydelig
avgrenset kompleksitet:

- **Ja (v1.4.0):**
  - `[McpPrompt]` attributt for å merke prompt‑metoder (med auto‑navngiving som for `[McpTool]`).
  - Prompt‑modeller (`PromptDefinition`, `PromptParameter`, `PromptMessage`).
  - Discovery/registrering av prompts via assembly‑scan (tilsvarende tools, men egen liste).
  - MCP‑metoder `prompts/list` og `prompts/get` i `ToolInvoker`.
  - Én eller to små eksempelprompts + tilhørende tester i examples/test‑prosjektene.
  - Enkel argument‑substitusjon (f.eks. `{name}` → verdi i `arguments`) inni prompt‑malene.

- **Senere versjoner (v1.5+ / v2.0):**
  - Avansert parameter‑typing / JSON Schema for prompts.
  - Persistente / dynamisk genererte prompts fra database eller config.
  - Mer avansert mal‑motor (f.eks. kondisjonale blokker, loops etc.).

Dette gir oss en første, fullverdig MCP Prompts‑støtte med tydelig API (`[McpPrompt]`, `prompts/list`, `prompts/get`),
uten å dra inn samme kompleksitetsnivå som en full Hybrid Tool API eller avansert schema‑generering.
