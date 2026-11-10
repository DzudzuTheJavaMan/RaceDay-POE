## Part 3 - MVC web application

RaceDay is a web platform for South African running, walking and cycling events. Organisers create and manage events, categories, enrolments and results. Participants browse events, enter an event under a category, and track their enrolments and race results.

### Roles
- **Organiser:** create, edit and delete events, upload event banners, manage categories, view and confirm enrolments, capture results.
- **Participant:** browse and filter upcoming events, view event details, enrol under a category, view their enrolments and personal results.

### Project structure
- `Part2/RaceDay.Api` - ASP.NET Core Web API (EF Core, SQL Server, session authentication, Swagger, Azure Blob Storage uploads)
- `Part2/RaceDay.Tests` - unit tests
- `Part3/RaceDay.Web` - ASP.NET Core MVC app. It has no database or Azure access; every operation goes through the API.
- `docs/` - ERD, endpoint plan and SQL script

### Run locally
1. API: `cd Part2/RaceDay.Api`, then `dotnet run` (Swagger at http://localhost:5149/swagger). For banner uploads, set the Azure connection string once with `dotnet user-secrets set "BlobStorage:ConnectionString" "<your connection string>"`.
2. Website: `cd Part3/RaceDay.Web`, then `dotnet run` (http://localhost:5101).

### Run with Docker
From the repository root:

`docker compose up --build`

The website is then available at http://localhost:5101 and the API at http://localhost:5149. To use banner uploads in Docker, add `BlobStorage__ConnectionString` under the `api` service environment in `docker-compose.yml` (not committed, to keep the key private).

### CI/CD
The GitHub Actions workflow (`.github/workflows/ci.yml`) builds the API and the MVC project, runs the unit tests and builds both Docker images.

![Green CI build](docs/ci-green-part3.png)

### Video
[Part 3 demo video]()

### Known limitations
- The Participant profile page and profile picture upload are not implemented.
- I could not run Docker locally because the lab VM does not support it. The Dockerfiles and `docker-compose.yml` are built in the CI workflow.

