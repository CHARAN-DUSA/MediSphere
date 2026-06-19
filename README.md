<div align="center">

# 🏥 MediSphere — Hospital & Doctor Appointment Platform

[![Angular](https://img.shields.io/badge/Angular-20-DD0031?style=for-the-badge&logo=angular&logoColor=white)](https://angular.dev/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--Time-512BD4?style=for-the-badge)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

**A full-stack hospital & doctor-appointment booking platform with dedicated, role-specific experiences for Patients, Doctors, Receptionists, and Admins.**

Covers the complete lifecycle of a clinic visit — discovery, booking, payment, live queueing, consultation, and post-visit reviews — plus the back-office tools (analytics, payouts, moderation) needed to run the platform.

[🏗️ Architecture](#-architecture) • [✨ Features](#-feature-guide-by-role) • [🔧 Tech Stack](#-tech-stack) • [🚀 Getting Started](#-getting-started) • [📡 API Reference](#-api-reference-by-controller)

</div>

---

## 📋 Table of Contents

- [About the Project](#-about-the-project)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [How a Booking Actually Works](#-how-a-booking-actually-works-end-to-end-flow)
- [Feature Guide by Role](#-feature-guide-by-role)
- [Payments, Commission & Payouts](#-payments-commission--payouts)
- [Rewards & Referral Program](#-rewards--referral-program)
- [Smart Doctor Recommendation Engine](#-smart-doctor-recommendation-engine)
- [Real-Time Features (SignalR)](#-real-time-features-signalr)
- [Authentication & Security](#-authentication--security)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
- [API Reference](#-api-reference-by-controller)
- [Available Scripts](#-available-scripts-frontend)
- [Security Notes](#-security-notes)
- [License](#-license)

---

## 🎯 About the Project

MediSphere is a clinic operations platform built around one core idea: **a booking is a transaction with real money, a real queue, and a real consultation behind it** — not just a calendar entry. The system handles slot locking to prevent double-booking, server-side payment fee splitting via Razorpay, live queue management over SignalR, and a deterministic (non-AI) doctor recommendation engine.

The backend is an **ASP.NET Core 8 Web API** built with Clean Architecture (Domain → Application → Infrastructure → API). The frontend is a fully **standalone-component Angular 20** application using Signals for state — no NgModules anywhere.

---

## 🔧 Tech Stack

### Backend — `MediSphere.API` (.NET 8, Clean Architecture)

| Layer | Responsibility | Key Libraries |
|---|---|---|
| **Domain** | Entities, enums, repository contracts | — |
| **Application** | Services, DTOs, validators, CQRS features | MediatR, AutoMapper, FluentValidation |
| **Infrastructure** | EF Core persistence, external integrations, real-time hubs | EF Core (SQL Server / PostgreSQL), Redis, Hangfire |
| **API** | Controllers, middleware, auth, Swagger | JWT Bearer Auth, Serilog, Swashbuckle |

### Frontend — `medisphere-ui` (Angular 20, standalone components)

| Concern | Approach |
|---|---|
| Components | Standalone components only — no `NgModules` |
| State | Angular Signals (`signal`, `computed`) and `input()` / `output()` for parent-child communication |
| Styling | `--ms-` prefixed CSS custom property design tokens (MediSphere design system) |
| Typography | Playfair Display (display) + DM Sans (body) |
| Real-time | `@microsoft/signalr` client wired into `signalr.service.ts` |
| HTTP | Functional interceptors for auth headers and centralized error handling |
| Routing | Lazy-loaded feature routes, guarded by `auth.guard.ts` and `role.guard.ts` |

### Platform-Wide Capabilities

| Capability | Details |
|---|---|
| 🔐 Auth | JWT (access + refresh token), role-based authorization (`Admin`, `Doctor`, `Patient`, `Receptionist`) |
| 🔒 Booking Integrity | Redis-backed short-lived lock per slot + DB-level conflict check — prevents double-booking |
| 💳 Payments | Razorpay integration, webhook + signature verification, `simulate-webhook` for local dev/testing |
| ⚡ Real-Time | SignalR for live queues, video-consultation signaling, in-app notifications |
| 🛠️ Background Jobs | Hangfire + an in-process task queue for post-payment emails/SMS/notifications |
| ✉️ Email & SMS | Brevo (API + SMTP relay), dedicated HTML template per event |
| 🧠 Recommendations | Deterministic, rule-based Smart Doctor Recommendation engine (no external AI/ML calls) |
| 🚀 Caching | Redis-backed slot caching + distributed locks, with a no-op fallback if Redis is unreachable |
| 📁 Storage | Local file storage for doctor profile images and patient medical records |
| 🪵 Observability | Centralized exception middleware, fixed-window rate limiting, structured logging via Serilog |
| 🗄️ Database | EF Core code-first migrations — SQL Server and PostgreSQL providers both supported |

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                  Angular 20 Frontend (Standalone)                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Patient  │  │ Doctor   │  │ Receptionist │  │    Admin     │ │
│  │ Features │  │ Features │  │  Features    │  │  Features    │ │
│  └──────────┘  └──────────┘  └──────────────┘  └──────────────┘ │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │     Core: Guards, Interceptors, Signals, SignalR Client    │  │
│  └───────────────────────────────────────────────────────────┘  │
└────────────────────────────┬───────────────────────────────────┘
                              │ HTTPS / JWT (+ SignalR over WSS)
┌────────────────────────────▼───────────────────────────────────┐
│                    ASP.NET Core 8 Web API                       │
│  ┌──────────┐  ┌────────────────┐  ┌─────────────────────────┐ │
│  │   API    │  │  Application   │  │      Infrastructure      │ │
│  │Controllers│ │ CQRS/DTOs/Valid│  │ EF Core, Redis, Hangfire │ │
│  └──────────┘  └────────────────┘  └─────────────────────────┘ │
│  ┌─────────────┐ ┌──────────────┐ ┌────────────────────────┐  │
│  │ SignalR Hubs│ │  Background  │ │  Razorpay / Brevo       │  │
│  │ Queue/Video/│ │  Task Queue  │ │  Integrations            │  │
│  │ Notification│ │              │ │                          │  │
│  └─────────────┘ └──────────────┘ └────────────────────────┘  │
└────────────────────────────┬───────────────────────────────────┘
                              │ EF Core
┌────────────────────────────▼───────────────────────────────────┐
│              SQL Server / PostgreSQL  +  Redis Cache             │
│  Users │ Doctors │ Patients │ Appointments │ Payments │ Reviews  │
└────────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

```
MediSphere/
├── MediSphere.Domain/          → Entities, enums (pure business objects)
├── MediSphere.Application/     → Services, DTOs, validators, CQRS features
├── MediSphere.Infrastructure/  → EF Core, SignalR hubs, Redis, Hangfire, email/SMS, payments
└── MediSphere.API/             → Controllers, middleware, Program.cs, appsettings
```

---

## 🔄 How a Booking Actually Works (end-to-end flow)

1. **Find a doctor** — browse with filters (specialty, department, gender, location, language, fee range, minimum rating, availability), or describe symptoms in the **Smart Finder**, which ranks doctors by relevance.
2. **Check availability** — selecting a doctor and date calls `GET /api/appointments/slots`, which reads the doctor's weekly `DoctorSchedule` (start time, end time, slot length), subtracts already-booked and cancelled slots, and caches the result in Redis for 30 minutes.
3. **Reserve & book** — booking a slot first checks a short-lived Redis lock key (`appointment:lock:{doctorId}:{date}:{time}`, 5-minute TTL) to stop two people booking the same slot in a race, then re-checks for a DB-level conflict before creating the appointment with status `Pending`.
4. **Pay** — the frontend calls `POST /api/payments/create-order` (Razorpay order), the patient pays via Razorpay Checkout, and Razorpay calls back the `/api/payments/webhook` endpoint with a signed payload. The backend verifies the signature, then:
   - Splits the gross fee into **admin commission, platform fee, tax, and net doctor payout**
   - Marks the appointment `Confirmed` and payment status `Paid`
   - Awards loyalty points and processes referral bonuses
   - Records a `PaymentTransaction` with the full breakdown
   - Queues emails/SMS and SignalR notifications to both patient and doctor on a background task queue, so the webhook response isn't held up
5. **Queue on the day** — each confirmed appointment gets a `QueueToken`. The doctor (or receptionist) uses **Call Next Patient** to advance the queue; the system marks any currently `InConsultation` patient `Completed` and promotes the next `Waiting` token, broadcasting the update over `QueueHub` so the patient's queue screen updates live.
6. **Consult** — for telemedicine appointments, both parties join a video session signaled through `VideoConsultationHub`.
7. **Close out** — the doctor marks the appointment `Completed` (or the system marks it `NoShow` if the time passes unattended). Completion emails go out, including a doctor-side earnings breakdown.
8. **Review** — the patient can leave a star rating + comment, which goes into a moderation queue before becoming public; the doctor can post one reply per review.

---

## ✨ Feature Guide by Role

### 👤 Patient

| Area | Details |
|---|---|
| **Account & Profile** | Register/login (BCrypt-hashed), JWT + 7-day refresh token, OTP password reset (6-digit, BCrypt-hashed, Redis-cached, 10-min expiry); **Family Members** to book on behalf of dependents; auto-generated referral code at signup |
| **Find a Doctor** | Filterable directory (specialty, department, gender, location, language, fee range, min. rating, availability); **Smart Symptom Finder** for plain-language search; save/unsave favorite doctors |
| **Book & Manage** | View live slots per doctor/day; book, reschedule, or cancel (subject to status rules); mark a booking as a follow-up; full history across `Pending`, `Confirmed`, `Completed`, `Cancelled`, `Rescheduled`, `NoShow`, `PendingPayment` |
| **Payments** | Razorpay checkout; itemized confirmation email (fee, commission, tax, net doctor amount); failed payments recorded and emailed |
| **Live Queue** | Real-time position/status (`Waiting`, `InConsultation`, `Completed`) pushed via SignalR — no refresh needed |
| **Health Records** | Upload documents (optionally linked to an appointment); view/delete own records; doctors & admins can view for clinical context |
| **Rewards & Referrals** | 10 points per paid booking; referral and welcome bonuses; full rewards statement |
| **Reviews & Notifications** | Star rating + review (moderated before going public); real-time in-app notifications with mark-as-read / mark-all-as-read |

### 🩺 Doctor

| Area | Details |
|---|---|
| **Onboarding** | Self-registration starts as `PendingReview`; only visible in search once admin-approved. States: `PendingReview`, `Approved`, `Rejected`, `Suspended`, `Blocked` |
| **Schedule** | Recurring weekly schedule per day (start/end time, slot duration); block specific slots; set vacation periods; toggle availability |
| **Daily Operations** | Six focused components — `doctor-stats`, `doctor-today`, `doctor-logs`, `doctor-schedule`, `doctor-reviews`, `doctor-profile`. Includes **Call Next Patient** and status updates that trigger tailored emails/notifications |
| **Earnings** | Dedicated `doctor-earnings` page: gross earnings, fees, taxes, commission, net earnings, visual breakdown via Angular signals; per-appointment financial history |
| **Reviews & Notifications** | View reviews, post one reply each; real-time notifications for bookings, payments, reschedules, cancellations, no-shows |

### 🛠️ Admin

| Area | Details |
|---|---|
| **Operations Dashboard** | Platform-wide KPIs (appointments, active doctors/patients/departments), financial KPIs (revenue, commission, payouts), appointment volume by department |
| **Doctor Management** | Approve/reject registrations, suspend/block/unblock accounts, full status listing |
| **Patient Management** | View all patients; block/unblock by email |
| **Department Management** | CRUD for hospital departments/specialties |
| **Content & Settings** | CMS-style content items; system-wide **commission rate, platform fee rate, tax rate** (configurable without code changes); broadcast announcements |
| **Review Moderation** | View and approve/reject pending reviews |
| **Analytics Suite** (`admin/reports`) | Revenue Report, Payout Analytics (fee/tax/commission/payout split, per-department/doctor breakdowns), Appointment Analytics, Doctor Analytics, Patient Analytics — all computed client-side via Angular `computed()` signals |

### 🧾 Receptionist

- Shares appointment-management permissions with Admin and Doctor on key endpoints
- View/manage appointments (confirm, reschedule, cancel, update status) on behalf of patients/doctors
- Operate the live queue — call next patient, update queue status during clinic hours

---

## 💰 Payments, Commission & Payouts

Every successful payment is split four ways using rates pulled live from **System Settings** (with safe defaults if a setting is missing):

| Component | Default Rate | Notes |
|---|---|---|
| Admin Commission | **15%** | Platform's cut for facilitating the booking |
| Tax | **18%** | Applied to the gross consultation fee |
| Platform Fee | **2%** | Covers payment processing / infrastructure |
| **Doctor Net Payout** | **65%** | `Gross − Commission − Platform Fee − Tax` |

These four numbers are calculated **server-side** at the moment a Razorpay webhook confirms a `payment.captured` event, and stored permanently on the `PaymentTransaction` record (`GrossAmount`, `AdminCommission`, `PlatformFee`, `TaxAmount`, `DoctorEarnings`, `NetDoctorAmount`) — so historical payouts don't change retroactively even if an admin later changes the rates.

The same split is mirrored on the frontend (admin Payout Analytics and doctor Earnings pages) as Angular `computed()` signals, purely for displaying breakdowns and visual bar charts from already-fetched totals — the authoritative calculation always happens on the backend.

> **Note:** one status-transition email template references a flat 65% doctor share independently of System Settings — if you change the commission/tax/fee rates, double-check that email copy still matches your configured numbers.

---

## 🎁 Rewards & Referral Program

| Rule | Value |
|---|---|
| Points earned per paid booking | **10 points** |
| Points earned by the referrer when their code is used | **100 points** |
| Welcome bonus for the new patient using a referral code | **50 points** |
| Point value | **1 point = ₹1** |
| Maximum redeemable discount | **50% of the doctor's consultation fee** |

- Every patient gets a unique referral code automatically at registration.
- Referral bonuses are only awarded on a patient's **first** successfully paid appointment.
- `GET /api/rewards/rules` exposes these constants publicly so the frontend never has to hardcode them.
- `GET /api/rewards/my-statement` returns a patient's current balance, referral code, and full point transaction history.

---

## 🧠 Smart Doctor Recommendation Engine

A transparent, rule-based matching engine — **not** a call to an external AI/ML service — making its behavior fully predictable and explainable:

1. **Keyword mapping** — free-text symptoms matched (case-insensitively) against a curated keyword map for seven specialties (Cardiology, Dermatology, Pediatrics, Orthopedics, Gynecology, Neurology, General Medicine).
2. **Fallback token search** — if no specialty keyword matches, input is tokenized, stop-words removed, and matched against doctor names, bios, qualifications, specialty, and department.
3. **Scoring** — every eligible doctor gets a score:
   - **+100** specialty/department match
   - **+up to 30** experience (2 pts/year, capped)
   - **+rating × 10** average star rating
   - **+20** currently available
   - **+up to 10** review volume (log-scaled)
4. Doctors returned **highest score first**.

Because it's deterministic, the same symptom text always ranks doctors the same way given the same underlying data — useful for debugging and for explaining results to users.

---

## ⚡ Real-Time Features (SignalR)

| Hub | Endpoint | Purpose |
|---|---|---|
| `QueueHub` | `/hubs/queue` | Live queue position/status updates as the doctor calls the next patient |
| `VideoConsultationHub` | `/hubs/video` | Telemedicine session signaling between patient and doctor |
| `NotificationHub` | `/hubs/notifications` | Push-style in-app notifications (bookings, payments, reschedules, cancellations, reviews) |

Because browsers can't attach custom headers to a WebSocket handshake, JWTs are also accepted via query string (`?access_token=...`) specifically for requests to `/hubs/*`.

---

## 🔐 Authentication & Security

- **JWT Bearer auth** — short-lived access token + 7-day refresh token, validated against `Jwt:Issuer`, `Jwt:Audience`, and signing key on every request
- **Roles** — `Admin`, `Doctor`, `Patient`, `Receptionist`, enforced per-endpoint via `[Authorize(Roles = "...")]`
- **Passwords** — BCrypt-hashed, complexity enforced on reset (min. 8 chars, uppercase, lowercase, number, special character)
- **Password reset** — 6-digit OTP, BCrypt-hashed before caching, 10-minute expiry, delivered by email; doesn't reveal whether an email exists in the database
- **Rate limiting** — `strict` (5 req / 10s, auth endpoints) and `general` (100 req / min, queue limit 10)
- **CORS** — locked to origins listed in `AllowedOrigins` configuration
- **Webhook integrity** — Razorpay payloads verified against `X-Razorpay-Signature` before any payment is processed

---

## 📁 Project Structure

```
MediSphere/
├── MediSphere.sln
├── MediSphere.Domain/          # Entities, enums, repository interfaces
├── MediSphere.Application/     # Services, DTOs, validators, CQRS features
├── MediSphere.Infrastructure/  # EF Core, SignalR hubs, Redis, Hangfire, email/SMS, payments
├── MediSphere.API/             # Controllers, middleware, Program.cs, appsettings
└── medisphere-ui/              # Angular 20 frontend
    └── src/app/
        ├── core/                # Guards, interceptors, services, models
        ├── features/
        │   ├── admin/           # Dashboard, doctor/patient/department/content management,
        │   │                    #   review moderation, broadcast, settings, analytics/reports
        │   ├── auth/            # Login, register (patient/doctor), password reset
        │   ├── dashboard/       # Doctor & patient dashboard shells
        │   ├── doctors/         # Listing, detail, earnings, schedule, reviews, profile,
        │   │                    #   plus the modular doctor dashboard (stats/today/logs)
        │   ├── patient/         # Appointments, favorites, health records, queue, rewards,
        │   │                    #   smart finder, notifications, profile
        │   ├── appointments/    # Booking flow & history
        │   ├── telemedicine/    # Video consultation page
        │   └── home/
        └── shared/              # Reusable UI components & utilities
```

---

## 🚀 Getting Started

### Prerequisites

```bash
# Check versions
dotnet --version       # 8.0.0 or higher
node --version          # 20.0.0 or higher
ng version              # Angular CLI ^20.0.0
```

```bash
npm install -g @angular/cli@20
```

Also required:
- SQL Server **or** PostgreSQL (one is required for `MediSphere.API`)
- Redis (optional locally — falls back to a no-op cache if unavailable; note that slot-booking locks and cached slot lookups won't be available without it)
- A [Razorpay](https://razorpay.com/) test account (for payment flows)
- A [Brevo](https://www.brevo.com/) account (for transactional email/SMS)

### 1. Clone & restore

```bash
git clone <your-repo-url>
cd MediSphere
dotnet restore
```

### 2. Configure the backend

> ⚠️ **Security note:** if this archive ships with an `appsettings.json` containing live-looking credentials (DB connection string, Razorpay test key/secret, Brevo API key + SMTP password), treat all of them as compromised — rotate/regenerate every one before doing anything else, and never commit real secrets to source control. Use `dotnet user-secrets` or environment variables for local development instead.

Set up your configuration in `MediSphere.API/appsettings.json` (or override via user secrets / environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Initial Catalog=MediSphereDb;...",
    "PostgreSQL": "Host=localhost;Database=MediSphereDb;Username=postgres;Password=...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "<a-strong-secret-at-least-32-characters>",
    "Issuer": "MediSphere.API",
    "Audience": "MediSphere.Client"
  },
  "AllowedOrigins": ["http://localhost:4200"],
  "AppBaseUrl": "https://localhost:5001",
  "FrontendBaseUrl": "http://localhost:4200",
  "Razorpay": {
    "KeyId": "<your-razorpay-key-id>",
    "KeySecret": "<your-razorpay-key-secret>",
    "WebhookSecret": "<your-webhook-secret>"
  },
  "Brevo": {
    "ApiKey": "<your-brevo-api-key>",
    "SenderEmail": "<sender@yourdomain.com>",
    "SenderName": "MediSphere Hospital",
    "Smtp": {
      "Host": "smtp-relay.brevo.com",
      "Port": 587,
      "Username": "<smtp-username>",
      "Password": "<smtp-password>",
      "EnableSsl": true
    }
  }
}
```

Recommended for local dev — keep secrets out of the repo entirely:

```bash
cd MediSphere.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your-local-dev-secret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

### 3. Apply database migrations

Migrations run automatically on startup (`db.Database.MigrateAsync()` in `Program.cs`), or run them manually:

```bash
cd MediSphere.Infrastructure
dotnet ef database update --startup-project ../MediSphere.API
```

### 4. Run the backend

```bash
cd MediSphere.API
dotnet run
```

API starts on `https://localhost:5001`. Swagger UI is available at `/swagger` in `Development`, with a built-in **Bearer** auth scheme for testing protected endpoints directly from the docs.

### 5. Configure & run the frontend

```bash
cd medisphere-ui
npm install
```

Update `src/environments/environment.ts` if your API isn't on the default port:

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

```bash
npm start
```

App is served at `http://localhost:4200`.

### 6. Try it out without real payments

You don't need a live Razorpay account to test the full booking flow locally: book an appointment, create the order as usual, then call `POST /api/payments/simulate-webhook` with the order ID, a fake payment ID, and the amount. This runs the exact same confirmation logic as a real webhook — fee splitting, loyalty points, referral bonuses, emails, and queue notifications.

---

## 📡 API Reference (by Controller)

All endpoints are prefixed `/api/{controller}` and require a valid JWT unless marked **Public**.

### `AuthController` — `/api/auth` 🔓 Public
Register patient/doctor, login, refresh token, forgot/reset password.

### `AppointmentsController` — `/api/appointments`

| Endpoint | Roles | Description |
|---|---|---|
| `GET /` | Admin, Doctor, Receptionist | List/filter appointments (paged) |
| `GET /my` | Patient, Receptionist | The current user's appointments |
| `GET /{id}` | Doctor, Admin, Receptionist | Single appointment |
| `POST /` | Patient | Book an appointment |
| `PUT /{id}` | Patient, Receptionist | Reschedule |
| `DELETE /{id}` | Patient, Receptionist | Cancel |
| `PATCH /{id}/status` | Doctor, Admin, Receptionist | Update appointment status |
| `GET /slots` | — | Available time slots for a doctor on a given date |

### `DoctorsController` — `/api/doctors`
Doctor directory/search, CRUD (Admin), profile image upload, schedule/vacation/slot-block management, earnings (`/{id}/earnings`), doctor-scoped notifications.

### `PatientsController` — `/api/patients`
Profile get/update, favorites, family members, patient-scoped notifications.

### `PaymentsController` — `/api/payments`

| Endpoint | Description |
|---|---|
| `GET /config` | Public sandbox-aware Razorpay key for the frontend checkout widget |
| `POST /create-order` | Creates a Razorpay order for an appointment |
| `POST /webhook` | Razorpay signature-verified webhook for `payment.captured` |
| `POST /simulate-webhook` | Dev/test helper to simulate a successful payment without Razorpay |
| `POST /payment-failed` | Records a failed payment attempt and notifies the patient |

### `MedicalRecordsController` — `/api/medicalrecords`
Upload (Patient), list by patient (Patient/Doctor/Admin), delete (Patient).

### `DepartmentsController` — `/api/departments`
CRUD for hospital departments (Admin for writes).

### `ReviewsController` — `/api/reviews`
Create (Patient), list by doctor (public), pending queue + moderate (Admin), respond (Doctor).

### `RewardsController` — `/api/rewards`
`my-statement` (Patient), `patient/{id}` (Admin), `rules` (Public).

### `SavedDoctorsController` — `/api/saveddoctors`
Save/remove a favorite doctor, list favorites, search within favorites.

### `QueueController` — `/api/queue`

| Endpoint | Roles | Description |
|---|---|---|
| `GET /doctor/{doctorId}` | — | Today's live queue for a doctor |
| `POST /call-next/{doctorId}` | Doctor, Receptionist, Admin | Advance the queue |
| `POST /update-status/{appointmentId}` | — | Update an individual queue entry's status |

### `SmartRecommendController` — `/api/smartrecommend`
`GET /?symptoms=...` — ranked doctor recommendations from free-text symptoms.

### `AdminController` — `/api/admin` 🛡️ Admin only
Dashboard stats, doctor approval/suspension/blocking, user blocking, patient listing, system settings, content management, broadcast messaging.

---

## 📜 Available Scripts (Frontend)

| Command | Description |
|---|---|
| `npm start` | Run the Angular dev server (`ng serve`) |
| `npm run build` | Production-style build (`ng build`) |
| `npm run build:prod` | Explicit production build (`ng build --configuration=production`) |
| `npm run watch` | Build in watch mode for development |

---

## 🔒 Security Notes

- Rate limiting is enforced via two named policies: `strict` (5 requests / 10s — auth endpoints) and `general` (100 requests / minute).
- CORS is restricted to origins listed under `AllowedOrigins` in configuration.
- All controllers (other than auth and a handful of explicitly public endpoints like `payments/config` and `rewards/rules`) require a valid JWT and are further scoped by role via `[Authorize(Roles = "...")]`.
- **Rotate any credentials that shipped in this codebase's `appsettings.json` before deploying or sharing this project.**

---

## 📜 License

Specify your license here (e.g. MIT, Apache 2.0, or proprietary/internal use only).

---

<div align="center">

**Built with Angular 20 Signals + ASP.NET Core 8 Clean Architecture.**

</div>