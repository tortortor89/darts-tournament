# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Full-stack darts tournament management application with:
- **Backend**: C# / ASP.NET Core 8.0 REST API with PostgreSQL (Entity Framework Core)
- **Frontend**: TypeScript / Angular 17.3 (standalone components architecture)

## Development Commands

### Frontend (`frontend/darts-tournament/`)
```bash
npm start              # Dev server at http://localhost:4200
npm run build          # Production build
npm test               # Run Karma tests
```

### Backend (`backend/`)
```bash
dotnet build
dotnet run --project DartsTournament.Api    # API at https://localhost:7228

# Database migrations
dotnet ef migrations add <Name> --project DartsTournament.Api
dotnet ef database update --project DartsTournament.Api
```

## Architecture

### Backend Structure (`backend/DartsTournament.Api/`)
- **Controllers/**: REST endpoints (Auth, Tournaments, Players, Matches)
- **Services/**: Business logic (AuthService for JWT/BCrypt, TournamentService)
- **Models/**: Domain entities (Tournament, Player, Match, Group, User, TournamentPlayer)
- **DTOs/**: Request/response contracts
- **Data/**: AppDbContext with PostgreSQL configuration

### Frontend Structure (`frontend/darts-tournament/src/app/`)
- **core/services/**: ApiService (HTTP), AuthService (JWT + Angular Signals)
- **core/interceptors/**: Auto-attaches Bearer token to requests
- **core/guards/**: Route protection
- **features/**: Feature components (auth, tournaments, players, matches)

### Key Patterns
- Angular standalone components (no NgModules)
- JWT authentication stored in localStorage (`auth_token`, `username`)
- AuthService uses Angular Signals for reactive auth state
- Backend uses service layer pattern with DTOs separate from domain models

## Configuration

### Backend (`appsettings.json`)
- PostgreSQL: `localhost:5432`, database: `dartstournament`
- CORS allows: `http://localhost:4200`

### Frontend
- API base URL hardcoded in ApiService: `https://localhost:7228/api`

## Domain Model

- **Tournament**: Formats (SingleElimination, RoundRobin, GroupStage), Status (Draft, InProgress, Completed)
- **Match**: Player scores, winner, optional group assignment
- **Group**: For group stage tournaments
- **TournamentPlayer**: Join table with optional seeding

## UI Language

The interface uses French labels.
