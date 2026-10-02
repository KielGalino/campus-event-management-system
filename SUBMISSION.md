# Online Campus Event Management System - Laboratory Midterm Submission

## Team Member Role Assignments
- **Member 1 (Jeanric Kiel Galino):** Systems Architect & Prompt Lead (Task 1 & Task 5)
- **Member 2 (Ashton Ivan Delapena):** Frontend Engineer (Task 2)
- **Member 3 (Hans Burton Damaso):** Database & Backend Engineer (Task 3)
- **Member 4 (Allen Alcanse):** QA & Security Engineer (Task 4)

---

## Task 1: Requirements Analysis & Prompt Architecture

### 1. Production-Grade RCTC Prompt
Please act as a Lead Systems Architect who designs software solutions for educational institutions. Our team of four undergraduate students is developing a working prototype for an Online Campus Event Management System during a three-hour laboratory examination using Visual Studio, Visual Studio Code, and a shared GitHub repository. The platform must allow students to view upcoming campus events and register for them, while allowing university administrators to review the registered attendees.   

Please design a high-level system architecture proposal focused only on initial system design. Do not write or generate the actual frontend, database, or backend source code yet. Instead, provide only:   

An overview of the three architectural tiers (Client, Server/API, Database) and how they communicate.   

The step-by-step data flow describing student event registration and administrator attendee retrieval.   

An explanation of how our assigned project files (/frontend/index.html, /database/schema.sql, /backend/RegistrationService.cs, /backend/ValidationTests.cs, and SUBMISSION.md) map to this architecture.   

A recommended lightweight technology stack suitable for rapid 3-hour prototyping with zero complex setup.   

Strict constraints:

Negative constraint: Do not use third-party state management libraries like Redux, and do not propose container orchestration or microservices such as Docker Compose or Kubernetes.   

Focus strictly on architecture, data flow, and file mapping; exclude full code implementations so our team can complete each development phase independently.   

Ensure all recommendations are realistic for four undergraduate developers to build and push to GitHub within 180 minutes.   

### 2. AI-Generated Output

## Three-Tier Architectural Pattern

The prototype utilizes an industry-standard, monolithic **Three-Tier Architecture** contained within a single repository to ensure immediate local execution without inter-service latency or network configuration issues.

```
+-------------------------------------------------------------------------+
|                        PRESENTATION TIER (Client)                       |
|           Browser Runtime | Pure HTML5, Semantic CSS, Native DOM        |
+------------------------------------+------------------------------------+
                                     |
                                     | HTTP / JSON (RESTful)
                                     v
+-------------------------------------------------------------------------+
|                       APPLICATION TIER (Server/API)                     |
|           ASP.NET Core Minimal API Engine | C# Business Logic           |
+------------------------------------+------------------------------------+
                                     |
                                     | Parameterized SQL (Direct DB Driver)
                                     v
+-------------------------------------------------------------------------+
|                          DATA TIER (Database)                           |
|             Embedded Relational Storage (Local File-Based)              |
+-------------------------------------------------------------------------+

```

### Component Responsibilities & Protocols

* **Presentation Tier (Client):**
* **Responsibilities:** Renders the responsive user interface for both student and administrative roles; captures form inputs; prevents invalid client submissions via native HTML validation; triggers asynchronous network dispatches; and dynamically alters document nodes based on API response payloads.
* **Protocol:** Communicates exclusively with the Application Tier using asynchronous HTTP/1.1 or HTTP/2 over standard local ports. Data exchange is strictly structured as UTF-8 encoded JSON payloads (`application/json`).


* **Application Tier (Server / API):**
* **Responsibilities:** Exposes uniform RESTful HTTP endpoints (`GET`, `POST`); parses incoming request bodies; handles CORS headers for local origin decoupling; executes domain validation and business constraints (e.g., duplicate detection, event capacity limits); controls database transaction lifecycles; and emits semantic HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `409 Conflict`).
* **Protocol:** Interacts with the Data Tier via a local embedded database connector using strongly-typed, parameterized SQL statements.


* **Data Tier (Database):**
* **Responsibilities:** Guarantees data persistence, referential integrity, and atomic transactions. Enforces schema-level rules such as primary keys, foreign key relationships, and compound unique constraints to protect against race conditions during registration.
* **Protocol:** In-process disk I/O via native driver routines, reading and writing to a shared local database file.



---

## Core End-to-End Data Flows

### Student Event Registration Sequence

1. **User Capture & Event Interception:** The student selects an upcoming event from the rendered list, inputs identification details (Student ID, Full Name, Institutional Email), and triggers form submission.
2. **Payload Serialization & Dispatch:** The client-side script intercepts the default browser submission event, serializes the input fields into a JSON document, and issues an asynchronous `POST /api/registrations` request to the backend.
3. **Request Ingestion & Routing:** The Application Tier receives the HTTP request, binds the JSON payload to an internal transfer object, and routes execution to the registration business logic.
4. **Domain Rule Validation:** The backend checks the business constraints in sequence:
* Verifies all mandatory attributes are present and correctly formatted.
* Queries the database to verify the referenced event exists and has available capacity.
* Queries the database to check if the Student ID has already registered for that specific Event ID.


5. **Atomic Persistence:** Upon passing validation, the backend initiates a parameterized SQL `INSERT` statement into the registration repository. The database commits the record and returns the newly generated unique registration identifier.
6. **Response Dispatch & UI Re-render:** The server responds with `201 Created` containing a confirmation object. The presentation tier receives the payload, clears the registration form fields, and dynamically displays an inline confirmation message and registration reference code to the student.

### Administrator Attendee Verification Sequence

1. **Query Trigger:** The administrator accesses the verification dashboard view and selects a specific target event from an event selector element.
2. **Data Fetch Request:** An administrative event handler issues an asynchronous `GET /api/events/{eventId}/attendees` request to the Application Tier.
3. **Data Retrieval & Relational Join:** The server extracts the `eventId` path parameter, validates its format, and executes a relational query joining the registration and student entities filtered by the event identifier.
4. **Data Projection & Response:** The returned relational records are mapped into an array of attendee projection objects (Student ID, Full Name, Institutional Email, Registration Timestamp) and returned to the caller with a `200 OK` status.
5. **View Update:** The client receives the JSON array, purges stale table row elements from the DOM, and iterates over the collection to construct and mount new attendee table rows in real time.

---

## Repository File Structural Mapping

```
/
├── /frontend/index.html            --> [Presentation Tier]
├── /database/schema.sql            --> [Data Tier]
├── /backend/RegistrationService.cs --> [Application Tier: Logic Engine]
├── /backend/ValidationTests.cs     --> [Cross-Cutting: Verification & QA]
└── SUBMISSION.md                   --> [System Runbook & Metadata]

```

* **`/frontend/index.html` (Presentation Tier):**
* *System Function:* Houses the complete single-page application structure. Contains semantic markup for the event catalog, the student registration form, and the administrative attendee verification table, paired with integrated vanilla JavaScript for client-side API communication and DOM updates.


* **`/database/schema.sql` (Data Tier):**
* *System Function:* The definitive Data Definition Language (DDL) initialization asset. Defines entity tables (`Events`, `Students`, `Registrations`), column types, primary keys, foreign key references, and a compound unique index on `(EventId, StudentId)` to prevent duplicate registrations. Also includes baseline seed data for immediate testing.


* **`/backend/RegistrationService.cs` (Application Tier):**
* *System Function:* The core domain component. Contains the application services responsible for processing registration requests, enforcing capacity ceilings, checking duplicate sign-ups, executing parameterized SQL queries, and mapping data records for the administrative endpoints.


* **`/backend/ValidationTests.cs` (Cross-Cutting QA):**
* *System Function:* Automated unit and integration testing suite. Directly exercises the methods in `RegistrationService.cs` against positive and negative test cases (e.g., successful sign-up, rejection of duplicate Student IDs, rejection when an event is at maximum capacity, invalid email validation).


* **`SUBMISSION.md` (Operational Runbook):**
* *System Function:* System documentation and execution specification. Details environment prerequisites, repository setup instructions, command-line steps to initialize the database from `schema.sql`, backend execution procedures, endpoint documentation, and team division of labor.



---

## Lightweight Rapid-Prototyping Technology Stack

This stack is selected specifically to minimize initial setup overhead, eliminate dependency conflicts across mixed developer environments, and run with zero external daemon configurations.

| Layer | Recommended Technology | Architectural Justification |
| --- | --- | --- |
| **Presentation Tier** | **Vanilla HTML5 + Modern JavaScript (ES6+ Native `fetch`)** | Operates natively in any browser with zero node tooling, build steps, bundlers, or package installation steps. Eliminates framework-induced runtime overhead. |
| **Application Tier** | **ASP.NET Core Minimal API (.NET 8 / 9)** | Provides high-throughput, low-ceremony HTTP routing directly in C#. Eliminates complex MVC controller scaffolding and runs natively in Visual Studio and VS Code via the standard .NET CLI (`dotnet run`). |
| **Data Tier** | **SQLite (`Microsoft.Data.Sqlite`)** | Fully self-contained, serverless, file-based relational database. Requires zero database server installation (unlike SQL Server, MySQL, or Postgres). The database lives directly as a local file, ensuring absolute portability across all team member clones. |
| **Test Engine** | **xUnit (or NUnit) via .NET Test SDK** | Native integration with Visual Studio Test Explorer and the `dotnet test` CLI command. Allows instant execution of `ValidationTests.cs` without extra test framework configuration. |

### 3. Manual Grounding Evaluation
The proposed monolithic three-tier architecture is realistic and achievable for our undergraduate team within the 180-minute examination window. By choosing native HTML5, an ASP.NET Core Minimal API, and an embedded SQLite database, the system avoids complex configuration steps, external servers, and state management libraries like Redux. The design directly maps each system layer to our assigned repository files—namely `index.html`, `schema.sql`, `RegistrationService.cs`, and `ValidationTests.cs`—which allows all four members to build and verify their respective components concurrently. Furthermore, the inclusion of three clear relational tables (`Events`, `Students`, and `Registrations`) and standard unit testing demonstrates that the architecture fully satisfies the laboratory requirements without introducing unmanageable technical debt.