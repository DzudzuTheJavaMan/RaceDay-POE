# RaceDay
RaceDay is a full-stack event management system that is tailored to the South African road running, walking and cycling community. It enables event organisers to create and manage events, event categories and participant results. Participants can browse through events, enter an event and manage their personal race history.
This repository contains the portfolio of evidence (POE), for PROG6212 - Programming 2B, incrementally assembled in three parts:
- **Part 1** - system planning: ERD, API endpoint plan and SQL database scripts
- **Part 2** - a RESTful API in C#, connected to the database, unit tests and CI/CD
- **Part 3** - an MVC web application consuming the API, with Azure Blob Storage and Docker
## Roles
- **Organiser** - can create, edit and delete events and event categories; capture participant results; view all event enrolments
- **Participant** - can create an account; browse events; enter a selected event in a selected category; view enrolments; view personal results
## Part 1 - System Planning and Database
All the planning documents in Part 1 can be found in the `/docs` folder:
- `RaceDay_ERD.png` - Entity Relationship Diagram showing all 6 entities (User, Profile, Event, Category, Enrolment, Result) with all primary keys, foreign keys and cardinality.
- `API_Endpoint_Plan.md` - Full endpoint plan covering Authentication, User Profile, Events, Categories, Enrolments, and Results.
- `RaceDay_Database.sql` - SQL script that creates the full database schema and seeds it with sample data. Tested and confirmed to run cleanly in SQL Server Management Studio (SSMS).
### Running the SQL script
1. Open SQL Server Management Studio (SSMS).
2. Open `docs/RaceDay_Database.sql`.
3. Execute the script. It creates the `RaceDayDB` database (if it doesn't already exist), drops and recreates all tables, and seeds sample data.
## CI/CD
A GitHub Actions workflow (`.github/workflows/part1-validation.yml`) runs on every push and validates that the `/docs` folder exists and contains all required Part 1 files.
**Build status screenshot:**
_<img width="1917" height="983" alt="Screenshot 2026-09-03 235444" src="https://github.com/user-attachments/assets/92860ddd-adb5-4ed6-a37f-0daa61ce0b2c" />
## Video

