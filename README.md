<div align="center">

# 🖨️ Printly

**Centralized assignment pipeline and print management for university networks**

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql)
![Entity Framework](https://img.shields.io/badge/EF_Core-8-512BD4?style=flat-square&logo=dotnet)
![SignalR](https://img.shields.io/badge/SignalR-Realtime-512BD4?style=flat-square&logo=dotnet)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-7952B3?style=flat-square&logo=bootstrap)

<br/>

<p>
  <a href="#the-problem">Problem</a> ·
  <a href="#the-solution">Solution</a> ·
  <a href="#features">Features</a> ·
  <a href="#tech-stack">Tech Stack</a> ·
  <a href="#architecture">Architecture</a> ·
  <a href="#getting-started">Getting Started</a> ·
  <a href="#database-schema">Database Schema</a> ·
  <a href="#api-reference">API Reference</a> ·
  <a href="#roadmap">Roadmap</a>
</p>

<p>By <a href="https://github.com/CosmosKyeremeh">BonGr8</a></p>

</div>

---

## The Problem

In many university classes across Ghana, students print assignments individually at a shared campus printer. This creates a severe operational bottleneck:

- **Disorganized Pipelines** — Print managers handle mixed, fragmented file formats manually with no tracking
- **Opacity** — No centralized record of who submitted, who paid, or what has been printed
- **Friction** — Manual cash handling and communication fragmented over WhatsApp
- **Deadline Failures** — Reminders sent per person with no broadcast system

This is not a minor UX inconvenience — it is a systemic coordination failure.

---

## The Solution

Printly transforms chaotic manual processing into a structured, trackable state machine:

```
Upload ──▶ Queue ──▶ Price ──▶ Pay ──▶ Print ──▶ Notify
```

Students submit once from any device. Admins manage everything — queue, pricing, payments, notifications — from a single dashboard.

---

## Features

### For Students

- **File Upload** — Drag and drop multiple files (PDF, DOCX, PPTX, XLSX, ZIP, images up to 50 MB)
- **Printing Instructions** — Leave per-file notes for the admin (copies, colour, edits needed)
- **File Conversion** — Convert PDF ↔ DOCX server-side via ConvertAPI
- **File Management** — View, download, delete uploaded files with live queue position and status
- **Payments** — Pay printing fees via MTN Mobile Money (Hubtel); view full payment history
- **Notifications** — Accordion inbox with unread/read separation; bell count updates in real time via SignalR
- **Resources** — Download templates and materials shared by the admin
- **Contact Rep** — Call, email, or WhatsApp the class rep directly from the app
- **Profile** — Update name, phone, WhatsApp number, and password

### For Admins

- **Print Queue** — Live queue for all submitted files; bulk select, mark printing / done / cancel
- **Pricing Engine** — Auto-calculates GHS 1 per page; manual price override with lock per file
- **Cash Payments** — Mark pending files as cash-paid with one tap
- **Direct Print** — Open any file and trigger the browser print dialog from the queue
- **Categories** — Create assignment types with optional deadlines; auto-triggers reminders
- **Notifications** — Broadcast announcements by type (deadline, payment, general, print ready)
- **Resources** — Upload templates and reference files for students
- **User Management** — View all users, change roles (Student ↔ Admin)
- **Invite Students** — Copy a shareable join link; students enroll with a class join code

### Platform

- **Multi-tenancy** — Each class is a fully isolated organization with its own join code and data
- **Role Hierarchy** — Platform Owner → Superadmin → Admin → Student
- **EF Core Query Filters** — Org-scoped data isolation enforced at the ORM layer on every query
- **Realtime Bell** — Unread count updates live via ASP.NET Core SignalR
- **Background Jobs** — Deadline reminders and scheduled tasks via Hangfire

---

## Tech Stack

| Layer | Technology | Purpose |
|---|---|---|
| Web Framework | ASP.NET Core 8 (MVC + Minimal API) | Controllers, routing, middleware pipeline |
| Language | C# 12 / .NET 8 | End-to-end type safety, async/await |
| Frontend | Razor Pages + Bootstrap 5 + Vanilla JS | Server-rendered UI, progressive enhancement |
| Realtime | SignalR | Live notification bell and queue updates |
| ORM | Entity Framework Core 8 | Code-first migrations, LINQ queries, query filters |
| Database | PostgreSQL 16 | Relational data, triggers, views |
| Auth | ASP.NET Core Identity | Email/password, role-based access, JWT |
| File Storage | Local disk (dev) / Azure Blob Storage (prod) | Per-user containers with access-controlled URLs |
| File Conversion | ConvertAPI (.NET SDK) | Server-side PDF ↔ DOCX conversion |
| Payments | Hubtel API (MTN MoMo) | Ghana-local mobile money |
| Email | MailKit | Transactional notifications and receipts |
| Background Jobs | Hangfire | Cron jobs, deadline reminders |
| Validation | FluentValidation | Request DTO validation pipeline |
| Testing | xUnit + Testcontainers | Unit and integration tests |
| Deployment | Railway / Azure App Service | CI/CD via GitHub Actions |

---

## Architecture

### Solution Structure

```
Printly.sln
├── Printly.Web/                  ← ASP.NET Core MVC + Razor Pages + API
│   ├── Controllers/              ← REST API endpoints
│   ├── Hubs/                     ← SignalR NotificationHub
│   ├── Pages/                    ← Razor Pages (student/, admin/, auth/)
│   ├── Middleware/               ← Org scoping, error handling
│   ├── wwwroot/                  ← Bootstrap 5, CSS, JS
│   └── Program.cs                ← DI registration, middleware pipeline
│
├── Printly.Core/                 ← Domain models, interfaces, DTOs
│   ├── Entities/                 ← Organization, AppUser, FileRecord, ...
│   ├── Interfaces/               ← IFileService, IPaymentService, ...
│   └── DTOs/                     ← Request/Response shapes
│
├── Printly.Infrastructure/       ← EF Core, repositories, integrations
│   ├── Data/                     ← PrintlyDbContext, migrations
│   ├── Repositories/             ← EF implementations
│   ├── Storage/                  ← Azure Blob / local disk abstraction
│   ├── Payments/                 ← Hubtel API client wrapper
│   └── Email/                    ← MailKit service
│
└── Printly.Tests/                ← xUnit unit + integration tests
```

### Multi-Tenancy Model

```
Organization (class / school)
    ├── Superadmin  — creates the org; receives join code to share
    ├── Admin       — class rep; manages queue, payments, notifications
    └── Student     — uploads files; tracks status; makes payments
```

Every entity (files, notifications, categories, payments) carries an `OrgId` foreign key. **EF Core Global Query Filters** automatically append `WHERE org_id = @currentOrgId` to every query — students can never see another org's data even if the API is called directly.

### File State Machine

```
Queued ──▶ Printing ──▶ Done
               │
               ▼
           Cancelled
```

State transitions are validated server-side in `QueueService`. Illegal transitions are rejected with HTTP 422. All transitions are timestamped and audited with the actioning user's ID.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- [Git](https://git-scm.com/)
- [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit

### VS Code Extensions

Install these before you open the project:

| Extension | ID | Purpose |
|---|---|---|
| C# Dev Kit | `ms-dotnettools.csdevkit` | IntelliSense, debugging, project management |
| C# (Base) | `ms-dotnettools.csharp` | Language support (installed with Dev Kit) |
| .NET Install Tool | `ms-dotnettools.vscode-dotnet-runtime` | Runtime management |
| NuGet Gallery | `patcx.vscode-nuget-gallery` | Browse and install NuGet packages |
| PostgreSQL | `cweijan.vscode-postgresql-client2` | Query your DB from inside VS Code |
| GitLens | `eamodio.gitlens` | Git blame, history, branch visualizer |
| Git Graph | `mhutchie.git-graph` | Visual git branch graph |
| Thunder Client | `rangav.vscode-thunder-client` | Test your API endpoints (like Postman, built-in) |
| Error Lens | `usernamehw.errorlens` | Inline error messages as you type |
| Bracket Pair Colorizer | `oderwat.indent-rainbow` | Indentation highlighting |
| Auto Rename Tag | `formulahendry.auto-rename-tag` | Useful for Razor `.cshtml` files |
| Path Intellisense | `christian-kohler.path-intellisense` | Autocomplete file paths |
| Todo Tree | `Gruntfuhrer.project-tree` | Track TODO/FIXME comments across the codebase |

### Local Setup

**1. Clone the repository**

```bash
git clone https://github.com/CosmosKyeremeh/printly-csharp.git
cd printly-csharp
git checkout develop
```

**2. Restore dependencies**

```bash
dotnet restore
```

**3. Configure environment**

Copy the example secrets file and fill in your values:

```bash
cp appsettings.Example.json Printly.Web/appsettings.Development.json
```

Fill in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=printly_dev;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters",
    "Issuer": "printly",
    "Audience": "printly-users",
    "ExpiryMinutes": 60
  },
  "ConvertApi": {
    "Secret": "your-convertapi-secret"
  },
  "Hubtel": {
    "ClientId": "your-hubtel-client-id",
    "ClientSecret": "your-hubtel-client-secret",
    "CallbackUrl": "https://localhost:7001/api/payments/webhook"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  },
  "Storage": {
    "Provider": "Local",
    "LocalPath": "uploads"
  }
}
```

**4. Run database migrations**

```bash
dotnet ef database update --project Printly.Infrastructure --startup-project Printly.Web
```

**5. Run the application**

```bash
dotnet run --project Printly.Web
```

Visit `https://localhost:7001`

---

## Database Schema

| Table | Description | Scope |
|---|---|---|
| `Organizations` | Schools or classes — multi-tenant root | Authenticated read; service write |
| `AspNetUsers` (extended) | Identity users + OrgId, Role, Phone, WhatsApp | Owner read/update; admin read org-wide |
| `Categories` | Assignment types with optional deadlines | Org-scoped; admin write |
| `FileRecords` | Uploaded assets — status, payment, page count, price | Owner read/write; admin read/update org |
| `PrintQueueItems` | Atomic state tracker per file upload | Owner read; admin full access org-wide |
| `Payments` | Transaction registry (MoMo / Cash) | Owner read; admin read org-wide |
| `Notifications` | Broadcast announcements with per-user read tracking | Org-scoped read; admin write |
| `AdminResources` | Templates and reference files for students | Org-scoped read; admin write |
| `FileComments` | Per-file thread between student and admin | Owner read/write; admin manage |

---

## API Reference

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register student or create org |
| `POST` | `/api/auth/login` | Public | Authenticate; returns JWT |
| `POST` | `/api/auth/join` | Student | Join org via join code |
| `GET` | `/api/files` | Scoped | List caller's files |
| `POST` | `/api/files/upload` | Student | Upload files (multipart) |
| `DELETE` | `/api/files/{id}` | Owner | Delete own file |
| `GET` | `/api/files/{id}/download` | Scoped | Download file |
| `POST` | `/api/files/{id}/convert` | Owner | Trigger PDF↔DOCX conversion |
| `GET` | `/api/queue` | Admin | Full org print queue |
| `PATCH` | `/api/queue/{id}/status` | Admin | Update file status |
| `POST` | `/api/payments/initiate` | Student | Initiate Hubtel MoMo charge |
| `POST` | `/api/payments/webhook` | Hubtel | Receive payment callback |
| `PATCH` | `/api/payments/{id}/cash` | Admin | Mark as cash-paid |
| `GET` | `/api/notifications` | Scoped | List notifications |
| `POST` | `/api/notifications` | Admin | Broadcast announcement |
| `POST` | `/api/notifications/read` | Student | Mark as read |
| `GET` | `/api/categories` | Scoped | List categories |
| `POST` | `/api/categories` | Admin | Create category |
| `GET` | `/api/resources` | Scoped | List resource files |
| `POST` | `/api/resources` | Admin | Upload resource file |
| `GET` | `/api/users` | Admin | List org users |
| `PATCH` | `/api/users/{id}/role` | Admin | Promote/demote user |
| `GET` | `/hubs/notifications` | Scoped | SignalR hub (realtime) |

---

## Security

| Concern | Status | Implementation |
|---|---|---|
| Org-scoped data isolation | ✅ | EF Core Global Query Filters on `OrgId` |
| Role-based authorization | ✅ | Policy-based with Identity + JWT claims |
| Payment status updates | ✅ | Server-side webhook only; signature verified |
| File access control | ✅ | Ownership check before every file stream |
| Secrets management | ✅ | User Secrets (dev) / Azure Key Vault (prod) |
| Input validation | ✅ | FluentValidation on all request DTOs |
| CSRF protection | ✅ | AntiForgery on all Razor Page form POSTs |
| Rate limiting | ✅ | ASP.NET Core Rate Limiting on auth endpoints |
| SQL injection | ✅ | Parameterized queries via EF Core |

---

## Git Flow

```
main          ← production only (tagged releases)
develop       ← default integration branch
feature/*     ← new work, branched from develop
release/*     ← version preparation
hotfix/*      ← emergency production fixes
```

**Commit convention:**

```
feat(queue): add bulk status update with optimistic UI
fix(auth): org_id not attached on signup
chore: bump version to 0.2.0
docs: update README with API reference
```

---

## Roadmap

### v0.1 — Core MVP
- [ ] Auth, org creation, join code flow
- [ ] File upload, queue, admin dashboard
- [ ] EF Core multi-tenant query filters
- [ ] Role-based authorization policies

### v0.2 — Payments
- [ ] Live Hubtel MoMo API integration
- [ ] Payment webhooks with automatic status updates
- [ ] PDF receipt generation via QuestPDF + MailKit delivery

### v0.3 — Notifications
- [ ] SignalR real-time notification bell
- [ ] Transactional email (print ready, payment confirmed)
- [ ] Automatic deadline reminders via Hangfire cron

### v0.4 — Polish
- [ ] In-browser PDF preview
- [ ] Auto page counter for price calculation
- [ ] Bulk ZIP download for admin
- [ ] Submission analytics charts

### v1.0 — Scale
- [ ] Multi-org superadmin dashboard
- [ ] Lecturer account type
- [ ] Flutter mobile app (iOS + Android)
- [ ] Direct printer integration via IPP protocol

---

## Contributing

1. Fork the repository
2. Create a feature branch from `develop`: `git checkout -b feature/your-feature`
3. Commit with conventional commits: `feat(area): description`
4. Push and open a PR targeting `develop`
5. Ensure CI passes before requesting review

---

## License

MIT — see [LICENSE](LICENSE) for details.
