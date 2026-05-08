# RESTful API Interview Prep — bijanp24 Repositories

Scanned repos: **DatingApp**, **CodelikelyDev**, **StoreLocatorDev**,
**roguenet-live-services**, **qubit-sim**, **event-mesh**.

Each section lists the endpoints found in the code, the tech stack used to
build or consume them, and a ready-to-use interview talking point.

---

## 1. DatingApp

**GitHub:** [bijanp24/DatingApp](https://github.com/bijanp24/DatingApp)  
**Tech:** C# · ASP.NET Core Web API · EF Core · JWT · Cloudinary · AutoMapper

### Endpoints

| Method | Route | What it does |
|--------|-------|-------------|
| `POST` | `/api/auth/register` | Register user, returns `CreatedAtRoute` with JWT |
| `POST` | `/api/auth/login` | Authenticate, return JWT + user object |
| `GET` | `/api/users` | Paginated user list filtered by gender (`[FromQuery] UserParams`) |
| `GET` | `/api/users/{id}` | Single user detail |
| `PUT` | `/api/users/{id}` | Update profile (auth-gated, ownership check) |
| `POST` | `/api/users/{id}/like/{recipientId}` | Like another user |
| `GET` | `/api/users/{userId}/messages` | Paginated inbox/outbox (`[FromQuery] MessageParams`) |
| `GET` | `/api/users/{userId}/messages/{id}` | Single message |
| `GET` | `/api/users/{userId}/messages/thread/{recipientId}` | Full message thread |
| `POST` | `/api/users/{userId}/messages` | Send message |
| `POST` | `/api/users/{userId}/messages/{id}` | Soft-delete message (sender/recipient flags) |
| `POST` | `/api/users/{userId}/messages/{id}/read` | Mark message read |
| `GET` | `/api/users/{userId}/photos/{id}` | Get single photo |
| `POST` | `/api/users/{userId}/photos` | Upload photo → Cloudinary, store URL + PublicId |
| `POST` | `/api/users/{userId}/photos/{id}/setMain` | Swap main photo |
| `DELETE` | `/api/users/{userId}/photos/{id}` | Delete photo from Cloudinary + DB |

### Key patterns

- **`[ApiController]` + `[Route("api/[controller]")]`** — conventional MVC attribute routing.
- **JWT Bearer** auth on all endpoints except `/api/auth/*`.
- **Repository pattern** (`IDatingRepository`, `IAuthRepository`) injected via constructor.
- **AutoMapper** converts domain models → DTOs before serialisation.
- **Cloudinary SDK** (`CloudinaryDotNet`) called synchronously inside the controller.
- **`Response.AddPagination()`** custom header extension for cursor metadata.
- **Nested resource routes** for photos and messages (`/api/users/{userId}/photos`).

### Interview talking points

> "In DatingApp I built a full social-profile API in ASP.NET Core using the MVC
> controller model. The trickiest part was the message threading — I used nested
> resource routes (`/users/{userId}/messages/thread/{recipientId}`) and soft-delete
> flags so neither party loses their copy until both have deleted it. Photo uploads
> go through Cloudinary: the controller validates ownership, calls the Cloudinary
> SDK, stores the returned URL and PublicId, then returns a `201 CreatedAtRoute`
> pointing to the new photo resource. Auth is JWT with claims-based ownership
> guards — every mutating endpoint verifies `ClaimTypes.NameIdentifier` against
> the route `{id}` before touching the database."

---

## 2. CodelikelyDev

**GitHub:** [bijanp24/CodelikelyDev](https://github.com/bijanp24/CodelikelyDev)  
**Tech:** C# · ASP.NET Core Web API · EF Core · Repository pattern

### Endpoints

| Method | Route | What it does |
|--------|-------|-------------|
| `GET` | `/api/blogposts` | All posts ordered by `PostId` descending |
| `GET` | `/api/blogposts/{id}` | Single post, `404` if missing |
| `POST` | `/api/blogposts` | Create post, returns `201 CreatedAtAction` |
| `PUT` | `/api/blogposts/{id}` | Update post, handles `DbUpdateConcurrencyException` |
| `DELETE` | `/api/blogposts/{id}` | Delete post, returns `200 Ok(deletedPost)` |

### Key patterns

- **Full CRUD** on a single resource — canonical REST example.
- **Generic `IDataRepository<T>`** (`Add`, `Update`, `Delete`, `SaveAsync`) wraps
  EF Core; the controller depends on the abstraction, not the context directly.
- **`[FromRoute]`** and **`[FromBody]`** explicit binding attributes.
- **`ModelState.IsValid`** guards on every mutating action.
- **`DbUpdateConcurrencyException`** handling with a `BlogPostExists()` existence
  check, returning `404` vs re-throwing.

### Interview talking points

> "CodelikelyDev is a clean CRUD API — GET list, GET by id, POST, PUT, DELETE —
> following the standard REST convention that POST returns `201 Created` and PUT
> returns `204 No Content`. I wrapped EF Core behind a generic `IDataRepository<T>`
> so the controller has no direct dependency on the DbContext; this makes unit
> testing the controller easy with a mock repository. The concurrency handling on PUT
> is a good example: it catches `DbUpdateConcurrencyException`, checks whether the
> resource still exists, and maps that to either `404 Not Found` or re-throws — a
> common pattern when optimistic concurrency is enabled on the entity."

---

## 3. StoreLocatorDev

**GitHub:** [bijanp24/StoreLocatorDev](https://github.com/bijanp24/StoreLocatorDev)  
**Tech:** TypeScript · Angular · Google Maps JavaScript API · Angular HttpClient

### External APIs consumed

| API / Service | How it is used |
|---------------|---------------|
| **Google Maps JavaScript API** (`maps.googleapis.com/maps/api/js`) | Loaded dynamically with `key` + `libraries=geometry,places`; renders the map canvas |
| **`google.maps.Map`** | Creates and controls the interactive map element |
| **`google.maps.Marker`** + **`InfoWindow`** | Drops pins for each store with a click popup showing address and phone |
| **`google.maps.Geocoder`** | Reverse-geocodes current lat/lng to a human address; also forward-geocodes typed addresses to coordinates |
| **`google.maps.places.Autocomplete`** | Address autocomplete input (US-restricted) |
| **`google.maps.geometry.spherical.computeDistanceBetween`** | Haversine-style distance calculation used in the geofence filter |
| **`google.maps.LatLngBounds`** | Auto-fits the map viewport to the set of found stores |
| **Browser Geolocation API** (`navigator.geolocation`) | Gets device coordinates for "current location" search |

### Key patterns

- **Dynamic script loading** — the Maps SDK is injected into `<body>` at runtime
  (not in `index.html`) so it only loads when the component activates.
- **Angular service as a cache layer** (`StoreLocatorService`) — stores the full
  dataset in memory and performs client-side geofence, state, and zip filtering;
  no server-side requests after initial data load.
- **Multiple search strategies** (current location, address, zip, state) unified
  behind a `SearchType` enum and a single `search()` dispatcher.
- **Paginated result display** — `resultsPerPage` slicing, increment/decrement
  page, markers updated on each page change.

### Interview talking points

> "StoreLocatorDev consumes the Google Maps JavaScript API at runtime using dynamic
> script injection — I load the SDK with the API key and the `geometry,places`
> libraries on component init rather than in the HTML head. That way the key is
> passed as an `@Input()` from the parent and the component is fully reusable.
> Distance filtering uses `google.maps.geometry.spherical.computeDistanceBetween`
> for accurate geodesic distance, which I convert to miles with a constant. The
> Geocoder is used in both directions: reverse-geocoding the user's GPS coordinates
> to a readable address, and forward-geocoding a typed address to lat/lng before the
> radius search. All of this is synchronous from the user's perspective — the store
> list is passed in once, then everything runs client-side."

---

## 4. roguenet-live-services

**GitHub:** [bijanp24/roguenet-live-services](https://github.com/bijanp24/roguenet-live-services)  
**Tech:** C# · .NET 10 · ASP.NET Core Minimal API · SQL Server · EF Core · Dapper · xUnit

### Endpoints

| Method | Route | What it does |
|--------|-------|-------------|
| `POST` | `/players` | Create player + starting profile (`1000` cash, level 1); `409 Conflict` on duplicate username |
| `GET` | `/players/{playerId:guid}/profile` | Fetch player stats (XP, level, cash, reputation, version) |
| `POST` | `/players/{playerId}/mission-completions` | Complete mission — idempotent reward grant, appends inventory ledger entry, fires outbox event |
| `GET` | `/players/{playerId}/inventory` | Read inventory from append-only transaction ledger |

### Key patterns

- **ASP.NET Core Minimal API** — no controllers; endpoints registered with
  `app.MapPost(…)` / `app.MapGet(…)` and `.WithName(…)` for OpenAPI.
- **Idempotency key** on mission completions — duplicate `completionId` returns
  the original response without double-granting rewards.
- **Append-only inventory ledger** — every reward is an immutable
  `InventoryTransaction` row; inventory balance is always derived, never stored.
- **Optimistic concurrency** (`Version` column) on `PlayerProfile` — concurrent
  requests can't corrupt the balance; the loser retries.
- **Outbox pattern** — a `Background Worker` reads the `OutboxEvents` table and
  relays events to downstream systems; the API never calls downstream directly.
- **Scalar API reference** served in Development for interactive docs.
- **Route constraint** `{playerId:guid}` — rejects malformed IDs at routing time.

### Interview talking points

> "RogueNet is my portfolio live-service backend. The centerpiece is the mission
> completion endpoint. It's idempotent by design: the client sends a client-generated
> `completionId`; if that ID has already been processed, the server returns the
> original response with no side effects — critical in mobile gaming where retries
> are common over flaky networks. Rewards land in an append-only inventory ledger;
> I never UPDATE the balance, so the ledger is auditable and roll-backs are trivial.
> To guard against concurrent grant races I use optimistic concurrency on the player
> profile — a `Version` field checked on every UPDATE. Downstream side effects (leaderboard
> updates, telemetry) go through the outbox: the API writes to `OutboxEvents` in the
> same DB transaction as the reward, then a background worker delivers them. This
> means a downstream failure can never cause a missed reward or a duplicate grant."

---

## 5. qubit-sim

**GitHub:** [bijanp24/qubit-sim](https://github.com/bijanp24/qubit-sim)  
**Tech:** Python · FastAPI · Pydantic · React (frontend) · Haskell (gate engine)

### Endpoints

| Method | Route | What it does |
|--------|-------|-------------|
| `POST` | `/simulate` | Accept `CircuitRequest` (n_qubits + list of gate operations), return statevector + probability distribution |
| `GET` | `/presets` | Return 5 built-in circuit presets (Bell State, GHZ, Superposition, Phase Kickback, 2-qubit QFT) |

### Request / Response models

```
CircuitRequest  { n_qubits: int, operations: [{ gate: str, qubits: [int], param?: float }] }
SimulationResponse { statevector: [{ re, im }], probabilities: [float], n_qubits, measurement_labels }
```

### Key patterns

- **FastAPI** with **Pydantic v2** models — automatic request validation and
  OpenAPI schema generation, zero boilerplate.
- **Thin REST adapter** pattern — the controller layer does nothing except
  translate HTTP types ↔ domain types; all computation is in `simulator.py`
  (Python) and the Haskell gate engine.
- **CORS middleware** — `allow_origins=["*"]` for local dev; the React frontend
  makes `fetch` calls to this service.
- **`response_model=`** annotation on `POST /simulate` — FastAPI validates the
  *output* schema, not just the input.
- **Optional parameter** `param` on gate ops — used for rotation gates (Rx, Ry,
  Rz, phase); absent for H, CNOT, X, S, SWAP.

### Interview talking points

> "qubit-sim is a REST wrapper around a quantum circuit simulator. The API is
> intentionally minimal: one POST endpoint that accepts a circuit definition and
> returns the full statevector and probability distribution. I used FastAPI because
> Pydantic handles both input validation and output serialisation — I declare the
> request and response models as Python dataclasses, and FastAPI generates the
> OpenAPI docs automatically. The heavier computation is in a separate Haskell
> gate engine; the Python layer just orchestrates it. The `/presets` endpoint
> is a good example of a GET-only discovery resource — it lets the React frontend
> populate a menu without any client-side configuration."

---

## 6. event-mesh

**GitHub:** [bijanp24/event-mesh](https://github.com/bijanp24/event-mesh)  
**Tech:** TypeScript · Express.js · NATS JetStream · Event Sourcing · CQRS · Saga pattern

### Endpoints

#### Order Service (port `3001`) — Command side

| Method | Route | What it does |
|--------|-------|-------------|
| `POST` | `/orders` | Place order → append `OrderPlaced` event to in-memory store → publish to NATS JetStream subject `events.order.orderplaced` |
| `GET` | `/orders/:id` | Reconstruct order state by replaying all events for that ID |
| `GET` | `/orders` | List all orders from in-memory aggregate cache |

#### Query Service — Read side (CQRS projection)

| Method | Route | What it does |
|--------|-------|-------------|
| `GET` | `/orders` | Return read-model projection built from NATS JetStream events |
| `GET` | `/orders/:id` | Return single order projection |

### Key patterns

- **CQRS** — Command service (write) and Query service (read) are separate
  Express apps that never share a database; they communicate only through NATS.
- **Event Sourcing** — `EventStore.append(type, aggregateId, payload)` appends
  immutable events; `EventStore.replay(id)` re-runs them to rebuild state —
  there is no UPDATE in the command store.
- **NATS JetStream** — durable pub/sub; the Order service publishes to
  `events.order.*`, the Query service subscribes and maintains its own read model.
- **Saga pattern** — distributed transaction coordination across the Order,
  Inventory, and Notification services via compensating events.
- **Aggregate hydration** — `hydrateOrder(events)` is a pure function; it takes
  an event stream and returns the current aggregate state.
- **In-memory BehaviorSubject cache** (`createStore`) for low-latency list
  reads; refreshed on every new event.

### Interview talking points

> "event-mesh demonstrates CQRS and event sourcing end-to-end. The command side
> is a tiny Express service: `POST /orders` validates the request, appends an
> `OrderPlaced` domain event to the event store, and publishes it to NATS JetStream
> — that's the only thing it does. The read side is a completely separate service
> that subscribes to NATS, applies projections, and exposes GET endpoints on its
> own read model. Because state is derived from events, the `GET /orders/:id` on
> the command side re-plays the event log on every call — useful for debugging and
> auditability. The saga pattern ties the order, inventory, and notification
> services together: if inventory reservation fails, a compensating event cancels
> the order, and the notification service sends the customer an apology. No shared
> database, no synchronous cross-service calls — just events."

---

## Quick-reference: Patterns by repo

| Pattern | Repos |
|---------|-------|
| JWT Authentication | DatingApp |
| Repository pattern | DatingApp, CodelikelyDev |
| Ownership / Claims guard | DatingApp |
| Paginated responses (custom headers) | DatingApp |
| Full CRUD | CodelikelyDev |
| Optimistic concurrency | CodelikelyDev (DbUpdateConcurrencyException), roguenet-live-services (Version) |
| Third-party API consumption | StoreLocatorDev (Google Maps) |
| Dynamic script loading | StoreLocatorDev |
| Client-side geofencing | StoreLocatorDev |
| ASP.NET Minimal API | roguenet-live-services |
| Idempotency keys | roguenet-live-services |
| Append-only ledger | roguenet-live-services |
| Outbox pattern | roguenet-live-services |
| FastAPI + Pydantic | qubit-sim |
| Thin REST adapter | qubit-sim |
| CQRS | event-mesh |
| Event Sourcing | event-mesh |
| NATS JetStream | event-mesh |
| Saga / compensating events | event-mesh |

---

## Common interview questions — mapped to your repos

| Question | Best repo to cite |
|----------|-------------------|
| "How do you handle authentication in REST?" | DatingApp — JWT, claims, per-action ownership |
| "How do you version or paginate API responses?" | DatingApp — custom `Pagination` response header |
| "Describe a full CRUD API you built." | CodelikelyDev — simple, clear, easy to walk through |
| "How do you consume a third-party API?" | StoreLocatorDev — Google Maps SDK, dynamic loading |
| "What is idempotency and why does it matter?" | roguenet-live-services — `completionId` dedup |
| "Explain the outbox pattern." | roguenet-live-services — DB-atomicity + background delivery |
| "What is CQRS?" | event-mesh — separate read/write services |
| "What is event sourcing?" | event-mesh — replay, no UPDATE, `hydrateOrder` |
| "How do you handle concurrent writes?" | roguenet-live-services (optimistic), CodelikelyDev (EF concurrency) |
| "Talk me through a FastAPI project." | qubit-sim — Pydantic models, thin adapter, auto-docs |
