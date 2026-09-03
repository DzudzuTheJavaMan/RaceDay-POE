# RaceDay API Endpoint Plan

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or Participant. | None (public) | { fullName, email, password, role } | 201 Created - user created, 400 Bad Request - invalid data, 409 Conflict - email already in use |
| POST | /api/auth/login | Authenticates a user and starts a session storing their user ID and role. | None (public) | { email, password } | 200 OK - session started, role returned, 401 Unauthorized - invalid credentials |
| POST | /api/auth/logout | Ends the current user's session. | Any (logged in) | None | 200 OK - session ended |
| GET | /api/profile | Retrieves the logged-in user's own profile information. | Any (logged in) | None | 200 OK - profile data, 404 Not Found - profile not set up |
| PUT | /api/profile | Updates the logged-in user's own profile information. | Any (logged in) | { phone, city, province, profilePictureUrl } | 200 OK - profile updated, 400 Bad Request - invalid data |
| GET | /api/events | Retrieves a list of all upcoming events. Available to both roles for browsing. | None (public) | None | 200 OK - list of events |
| GET | /api/events/{id} | Retrieves full details for a specific event, including its categories. | None (public) | None | 200 OK - event details, 404 Not Found - event does not exist |
| POST | /api/events | Creates a new event. | Organiser | { name, description, eventDate, location, distance, eventType } | 201 Created - event created, 400 Bad Request - invalid data, 403 Forbidden - not an Organiser |
| PUT | /api/events/{id} | Updates an existing event owned by the logged-in Organiser. | Organiser | { name, description, eventDate, location, distance, eventType } | 200 OK - event updated, 403 Forbidden - not the event owner, 404 Not Found - event does not exist |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 200 OK - event deleted, 403 Forbidden - not the event owner, 404 Not Found - event does not exist |
| POST | /api/events/{id}/banner | Uploads a banner image for an event, stored in Azure Blob Storage. | Organiser | Form-data image file | 200 OK - image URL returned, 403 Forbidden - not the event owner |
| GET | /api/events/{id}/categories | Retrieves all categories available for a specific event. | None (public) | None | 200 OK - list of categories, 404 Not Found - event does not exist |
| POST | /api/events/{id}/categories | Adds a new age or distance category to an event. | Organiser | { name } | 201 Created - category created, 403 Forbidden - not the event owner |
| PUT | /api/categories/{id} | Updates an existing category. | Organiser | { name } | 200 OK - category updated, 403 Forbidden - not the event owner, 404 Not Found - category does not exist |
| DELETE | /api/categories/{id} | Deletes an existing category. | Organiser | None | 200 OK - category deleted, 403 Forbidden - not the event owner, 404 Not Found - category does not exist |
| POST | /api/enrolments | Enrols the logged-in Participant into an event under a selected category. | Participant | { eventId, categoryId } | 201 Created - enrolment recorded, 403 Forbidden - not a Participant, 409 Conflict - already enrolled |
| GET | /api/enrolments/me | Retrieves all enrolments for the logged-in Participant. | Participant | None | 200 OK - list of enrolments |
| GET | /api/events/{id}/enrolments | Retrieves all Participant enrolments for a specific event owned by the Organiser. | Organiser | None | 200 OK - list of enrolments, 403 Forbidden - not the event owner |
| POST | /api/results | Captures a finish time and finishing position for a Participant's enrolment. | Organiser | { enrolmentId, finishTime, position } | 201 Created - result recorded, 403 Forbidden - not the event owner, 409 Conflict - result already captured |
| GET | /api/results/me | Retrieves the logged-in Participant's personal race history and results. | Participant | None | 200 OK - list of results with event name, date, category, finish time, position |
