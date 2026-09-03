# Appointment Manager

An appointment booking app with two ways to book: directly from the UI, or by chatting with an AI assistant that does the same thing through tool calls. Backend: C# / .NET 8. Frontend: Next.js + Radix Themes. AI: any OpenAI-compatible endpoint via LiteLLM. Calendar: optional Google Calendar sync.

## Features

- **Browse & book directly** — home page lists providers; expand one to see open slots grouped by date, click a slot, fill in name/email, done.
- **Or just ask the assistant** — a chat panel docked on the side lets you book, reschedule, or cancel in plain language. It resolves provider/slot names to the right records itself via tool calls (`list_providers`, `get_available_slots`, `book_appointment`, `reschedule_appointment`, `cancel_appointment`, `send_confirmation`) — it never asks you for an internal ID.
- **Google Calendar sync (optional)** — booking/rescheduling/cancelling an appointment through either path also creates/updates/deletes an event on one shared Google Calendar, once connected. The app works fully without it.
- **Conversation memory** — each chat session's history is persisted in SQLite, so a page reload doesn't lose context.

## Stack

- **Backend**: ASP.NET Core 8 Web API, EF Core + SQLite
- **Frontend**: Next.js (App Router, TypeScript), [Radix Themes](https://www.radix-ui.com/themes) for UI, DM Sans
- **AI**: any OpenAI-compatible `/chat/completions` endpoint, via a LiteLLM-style proxy — model/provider is just config
- **Calendar**: Google Calendar API (OAuth2, one shared calendar)

## Project structure

```
backend/
  AppointmentManager.Api/
    Controllers/     ChatController, AppointmentsController, ProvidersController,
                      SlotsController, GoogleAuthController
    Agent/            AgentOrchestrator (the tool-calling loop), LiteLlmClient,
                      Agent/Tools/ (tool schemas + implementations)
    GoogleCalendar/   GoogleCalendarService, OAuth options
    Data/             AppDbContext, entities, EF Core migrations, seed data
  AppointmentManager.Tests/   xUnit tests for the booking tools

frontend/
  app/                page.tsx (home), layout.tsx (fonts, Radix Theme, gradient bg)
  components/         Header, ProvidersPanel, SlotGrid, BookingDialog,
                      ChatWindow, MessageBubble
  lib/api.ts          fetch wrappers for the backend API
```

## Run the backend

```bash
cd backend/AppointmentManager.Api
dotnet run
```

Runs on `http://localhost:5080`, applies EF Core migrations, and seeds three providers (Dr. Alice, Dr. Bob, Dr. Chen) with a week of availability. SQLite DB file (`appointments.db`) is created alongside the project and is gitignored.

## Secrets

All secrets (LiteLLM API key, Google Calendar OAuth credentials) live in a single `.env` file at the repo root — gitignored, never committed. Copy the template and fill it in:

```bash
cp .env.example .env
```

```bash
# .env
GoogleCalendar__ClientId=....apps.googleusercontent.com
GoogleCalendar__ClientSecret=...

LiteLlm__ApiKey=sk-...
LiteLlm__BaseUrl=http://localhost:4000
LiteLlm__Model=gpt-4o-mini
```

The backend loads `.env` automatically at startup (via `DotNetEnv`, searching upward from the working directory) and maps `Section__Key` to the matching `appsettings.json` key — no code or config changes needed when you add a value. `appsettings.Development.json` is also gitignored if you'd rather use that instead.

## Google Calendar sync (optional)

Every booked/rescheduled/cancelled appointment can sync to a single shared Google Calendar. It's optional — the app works fully without it (sync is skipped and logged, not required).

1. In [Google Cloud Console](https://console.cloud.google.com/), create a project, enable the **Google Calendar API**, and create an **OAuth 2.0 Client ID** (Web application) with authorized redirect URI `http://localhost:5080/api/google/auth/callback`. While the OAuth consent screen is in "Testing" status, add your own Google account under **Audience → Test users**.
2. Add the Client ID/Secret to `.env` (see above).
3. With the backend running, visit `http://localhost:5080/api/google/auth/login` in a browser and grant consent (one-time, for whichever Google account owns the shared calendar). Check `http://localhost:5080/api/google/auth/status` to confirm `"connected": true`.

## Run the frontend

```bash
cd frontend
npm install
cp .env.local.example .env.local   # points at the backend, edit if needed
npm run dev
```

Runs on `http://localhost:3000` by default (pass `-- -p 3001` to use another port — the backend's CORS policy already allows both 3000 and 3001 for local dev).

## Run tests

```bash
cd backend
dotnet test
```

Covers the booking tool implementations (`book_appointment`, `reschedule_appointment`, `cancel_appointment`) against a real in-memory SQLite database.
