# P&M Project Architecture

## 1. Mobile Frontend (MVVM Architecture)

**Purpose:** User interface for students and mentors.

### Components

- **View (Screens)**
  - Registration/Login
  - Mentor search & filter
  - Mentor profile & ratings
  - Chat interface (real-time)
  - Rating & comment forms
  - Only responsible for displaying data, no business logic

- **ViewModel**
  - Handles user interactions from View
  - Prepares data for View
  - Sends requests to Model for data fetching/saving
  - Examples:
    - Filter mentors by subject & rating
    - Send chat messages
    - Compute average rating

- **Model**
  - Contains data structures & business logic (locally or via backend)
  - Examples: User, Student, Mentor, ChatMessage, Review
  - Communicates with backend via REST API
  - Handles local validation and simple processing

### Technologies
| Option | Pros | Notes |
|--------|------|-------|
| Flutter (Dart) | Cross-platform | Need to learn Dart |
| React Native (JS/TS) | Cross-platform | OOP demonstration weaker |
| Native Android (Kotlin/Java) | Full Android support | Separate iOS app needed |
| Native iOS (Swift) | Full iOS support | Separate Android app needed |

### Data Flow
1. Student opens screen → View
2. View asks ViewModel → “get mentors for math”
3. ViewModel calls backend → receives mentor list
4. ViewModel prepares data → View shows to user
5. Chat: View → ViewModel → Model → backend → database → back

---

## 2. Backend (C# + .NET / ASP.NET Core)

**Purpose:** Process requests from frontend, enforce business logic, interact with database.

### Layered Structure

1. **Controllers**
   - Handle incoming HTTP requests
   - Examples: `MentorController`, `ChatController`, `UserController`

2. **Services (Business Logic Layer)**
   - Implement app rules
   - Examples:
     - Filtering mentors
     - Starting chat sessions
     - Calculating ratings
     - Validating user actions

3. **Repositories (Data Access Layer)**
   - Handle direct database communication
   - Examples: `MentorRepository`, `ChatMessageRepository`, `ReviewRepository`

4. **Models (Server-side)**
   - User, Mentor, ChatMessage, Review, Subject
   - Used for ORM mapping and API responses

### Features
- REST API endpoints
- Authentication & authorization
- Error handling (custom exceptions)
- Multi-threading for chat & real-time updates
- Unit and integration tests

---

## 3. Database (PostgreSQL)

**Purpose:** Store all persistent data.

### Tables / Entities
- `users` – general user info (student & mentor)
- `mentors` – mentor-specific info (subjects, rating, sessions)
- `subjects` – subjects for filtering
- `sessions` – active chat sessions
- `messages` – chat messages
- `reviews` – ratings and comments

### Features
- ER Diagram with relationships:
  - User → Mentor (1-to-1)
  - Mentor → Subject (many-to-many)
  - Session → Messages (1-to-many)
  - Session → Review (1-to-1)
- Normalized structure
- ORM: Entity Framework
- Migrations included

---

## 4. Infrastructure & DevOps

**Purpose:** Deployment, scaling, security, and maintenance.

### Components
1. **Containerization**
   - Docker for backend + database
   - Isolated environments

2. **Orchestration**
   - Kubernetes (or similar) for scaling and container management
   - Handles automatic restart & load balancing

3. **CI/CD**
   - Continuous Integration: automatic tests on Git push
   - Continuous Deployment: auto-deploy new versions
   - Pre-commit hooks: prevent secrets in Git

4. **Observability & Security**
   - Logs & metrics for API usage, chat sessions, errors
   - Alerts on failures
   - Secrets management (passwords, API keys)

---

## 5. Data Flow Example

**Search Mentor Flow:**
1. Student opens mentor search → View
2. View asks ViewModel → “get mentors for math”
3. ViewModel sends REST API request → Backend Controller
4. Controller calls Service → Service calls Repository → fetch from Database
5. Data flows back: Database → Repository → Service → Controller → ViewModel → View
6. Student sees mentor list on screen

**Chat Flow:**
1. Student sends message -> View -> ViewModel -> Model -> Backend -> Database
2. Mentor receives message -> Database -> Backend -> Model -> ViewModel -> View