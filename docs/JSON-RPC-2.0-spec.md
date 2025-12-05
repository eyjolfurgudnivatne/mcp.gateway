# JSON-RPC 2.0 Specification (Sammendrag)

Kilde: [https://www.jsonrpc.org/specification](https://www.jsonrpc.org/specification)

## Protokoll

- JSON-RPC 2.0 er en stateless, light-weight remote procedure call (RPC) protokoll.
- Kommunikasjon skjer via JSON-objekter.

## Meldingsstruktur

### Request Object

```
{
  "jsonrpc": "2.0",
  "method": "methodName",
  "params": { ... } | [ ... ],
  "id": 1
}
```
- `jsonrpc`: Må være `"2.0"`.
- `method`: Navn på metoden som skal kalles (string, kreves).
- `params`: Parameter(e) til metoden (object eller array, valgfritt).
- `id`: Unik identifikator for request (string, number, eller null, kreves for request, utelates for notification).

### Response Object

```
{
  "jsonrpc": "2.0",
  "result": { ... },
  "id": 1
}
```
- `jsonrpc`: Må være `"2.0"`.
- `result`: Resultatet av metoden (valgfritt, kreves hvis ingen error).
- `id`: Samme som i request.

### Error Object

```
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32600,
    "message": "Invalid Request",
    "data": null
  },
  "id": 1
}
```
- `error`: Objekt med følgende felter:
  - `code`: Integer, standardiserte og egendefinerte feilkoder.
  - `message`: String, beskrivelse av feilen.
  - `data`: Ekstra data om feilen (valgfritt).
- `id`: Samme som i request, eller null hvis request ikke kunne tolkes.

## Notification

- En notification er en request uten `id`.
- Serveren skal ikke svare på notifications.

## Feilkoder

- -32700: Parse error
- -32600: Invalid Request
- -32601: Method not found
- -32602: Invalid params
- -32603: Internal error
- -32000 til -32099: Server error (reservert for implementeringer)

## Batch

- Flere requests kan sendes som et array.
- Responsen er et array med svar for hver request.

## Eksempel på batch-request

```
[
  { "jsonrpc": "2.0", "method": "sum", "params": [1,2,4], "id": "1" },
  { "jsonrpc": "2.0", "method": "notify_hello", "params": [7] },
  { "jsonrpc": "2.0", "method": "subtract", "params": [42,23], "id": "2" }
]
```

## Viktige regler

- Alle meldinger må ha `"jsonrpc": "2.0"`.
- `id` må være unik per request.
- Serveren må returnere enten `result` eller `error`, aldri begge.
- Notifications skal ikke gi respons.

---

Denne filen kan brukes av Copilot for å validere, sparre og generere tester i henhold til JSON-RPC 2.0 spesifikasjonen.
