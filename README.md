# Vehicle Status Management System

A full-stack vehicle status management system built with **ASP.NET Core Web API**, **SQL Server**, and **Vue 3**.

The project follows a layered **Controller - Service - Repository** backend architecture and a component-based Vue frontend. Version 2 adds keyword search, status filtering, server-side pagination, vehicle detail pages, historical status charts, and a database-driven dashboard. It also includes JWT authentication, role-based authorization, DTO validation, centralized exception handling, and service-layer unit tests.

---

## Highlights

- Full-stack architecture: **Vue 3 + ASP.NET Core Web API + SQL Server**
- JWT-based login and protected API access
- Role-based authorization for **User** and **Admin**
- Vehicle CRUD with backend business-rule validation
- Vehicle status history with one-to-many database relationships
- Keyword search and vehicle-status filtering
- SQL Server pagination with total counts and configurable page size
- Vehicle details, status-history table, and ECharts trend visualizations
- Database-driven dashboard with vehicle counts and status distribution
- Vue component separation with **Props / Emits**
- Centralized Axios instance with JWT request interceptor
- Vue Router with authentication navigation guard
- Element Plus management UI
- xUnit + Moq service-layer unit tests

---

## Features

### Vehicle Management

- Query all vehicles
- Query vehicle by ID
- Create vehicle information
- Update vehicle information
- Delete vehicle information
- Prevent duplicate plate numbers
- Prevent deletion of vehicles with historical status records
- Reload frontend data from the backend after successful write operations
- Search by partial plate number or model (`keyword`)
- Filter by vehicle status (`status`)
- Use `GET /api/vehicles/paged` for server-side pagination (page, pageSize, totalCount)
- View a vehicle detail page at `/vehicles/:id`

### Vehicle Status Management

- Add vehicle status records
- Query vehicle status history
- Record temperature, speed, battery level, and timestamp
- Maintain a one-to-many relationship between vehicles and status records
- Allow Admin users to add new status records while authenticated users can view history
- Display temperature, speed, and battery trends with ECharts

### Dashboard

- Show total vehicles, Active vehicles, Inactive vehicles, and total status records
- Display Active / Inactive distribution using an ECharts doughnut chart
- Refresh summary values from `GET /api/dashboard/summary`
- The dashboard represents database snapshots, not live telemetry

### Authentication & Authorization

- User registration
- User login
- Password hashing
- JWT Bearer authentication
- Role-based authorization
- `User` and `Admin` roles
- Protected frontend routes
- Automatic JWT injection through an Axios request interceptor

Permission model:

| Operation | User | Admin |
| --- | :---: | :---: |
| View vehicles | ✓ | ✓ |
| View vehicle status | ✓ | ✓ |
| View dashboard | ✓ | ✓ |
| Add vehicle status record | ✗ | ✓ |
| Create vehicle | ✗ | ✓ |
| Update vehicle | ✗ | ✓ |
| Delete vehicle | ✗ | ✓ |

### Validation & Exception Handling

- DTO validation with `DataAnnotations`
- Service-layer business-rule validation
- Global exception handling middleware
- Frontend error handling based on HTTP status codes

| Status Code | Scenario |
| --- | --- |
| `200` | Request succeeded |
| `201` | Resource created |
| `204` | Resource deleted |
| `400` | Invalid request parameters |
| `401` | Authentication failed / token invalid |
| `403` | Authenticated but insufficient permissions |
| `404` | Resource not found |
| `409` | Business rule conflict |
| `500` | Internal server error |

---

## Frontend

The frontend is built with **Vue 3**, **Vite**, **Axios**, **Vue Router**, **Element Plus**, and **ECharts**.

### Login Page

The login page:

- Collects username and password
- Calls `POST /api/auth/login`
- Stores the returned JWT, username, and role in `localStorage`
- Redirects authenticated users to the vehicle management page
- Displays validation and login error messages

### Vehicle Management Page

The vehicle page provides:

- Vehicle table display
- Add vehicle dialog
- Edit vehicle dialog
- Delete confirmation dialog
- Loading state
- Empty-state display
- User / Admin role badge
- Logout
- Admin-only Create / Edit / Delete controls

### Frontend Routing

Current routes:

```text
/login
/vehicles
/vehicles/:id
/dashboard
```

The Vue Router navigation guard redirects unauthenticated users to `/login`.

### Component Structure

```text
src/
├── components/
│   ├── LoginForm.vue
│   ├── VehicleTable.vue
│   ├── VehicleDialog.vue
│   └── VehicleStatusChart.vue
│
├── views/
│   ├── LoginView.vue
│   ├── VehicleView.vue
│   ├── VehicleDetailView.vue
│   └── DashboardView.vue
│
├── router/
│   └── index.js
│
├── services/
│   └── api.js
│
├── App.vue
└── main.js
```

The frontend uses:

- **Props** to pass state from parent components to child components
- **Emits** to send user actions back to parent components
- Component `v-model` for reusable form bindings
- An Axios instance for centralized backend configuration
- Request interceptors to attach JWT Bearer tokens automatically
- Response interception for common authentication cleanup

### Frontend Preview

Current UI includes:

1. **Login Page** - Element Plus login card
2. **Vehicle Management Page** - CRUD, search, status filtering, and pagination
3. **Vehicle Detail Page** - details, historical records, and status trend chart
4. **Dashboard Page** - statistics and status distribution
5. **Add / Edit Vehicle Dialog** and delete confirmation

Existing screenshots below cover the original login and vehicle management flows. Updated V2 screenshots can be added under `docs/images/`.

### Screenshots

### Login Page

![Login Page](docs/images/login-page.png)

### Vehicle Management

![Vehicle Management](docs/images/vehicle-management.png)

### Add / Edit Vehicle Dialog

![Vehicle Dialog](docs/images/vehicle-dialog.png)

---

## Tech Stack

### Backend

- **C#**
- **ASP.NET Core Web API**
- **SQL Server**
- **ADO.NET / Microsoft.Data.SqlClient**
- **JWT Bearer Authentication**
- **ASP.NET Core Identity PasswordHasher**
- **Async / Await**
- **Dependency Injection**
- **RESTful API**
- **OpenAPI**

### Frontend

- **Vue 3**
- **Vite**
- **Axios**
- **Vue Router**
- **Element Plus**
- **ECharts**
- **localStorage**
- **JWT Bearer Token**

### Testing & Development

- **xUnit**
- **Moq**
- **Git / GitHub**
- **Ubuntu / Linux**
- **VS Code**

---

## Architecture

### Overall Architecture

```text
Browser
   |
   v
Vue 3 Frontend
   |
   | Axios / JWT Bearer Token
   v
ASP.NET Core Web API
   |
   v
Authentication / Authorization
   |
   v
Controller
   |
   v
Service
   |
   v
Repository
   |
   v
SQL Server
```

### Backend Request Pipeline

```text
HTTP Request
     |
     v
Global Exception Middleware
     |
     v
Authentication
     |
     v
Authorization
     |
     v
Controller
     |
     v
Service
     |
     v
Repository
     |
     v
SQL Server
```

Responsibilities:

- **Controller**
  - Receives HTTP requests
  - Performs model binding
  - Returns HTTP responses

- **Service**
  - Handles business logic
  - Enforces business rules
  - Converts between DTOs and Models

- **Repository**
  - Handles database access
  - Executes SQL through ADO.NET

- **Middleware**
  - Handles global exceptions
  - Produces unified error responses

- **Vue Frontend**
  - Displays data and forms
  - Sends REST API requests
  - Manages frontend route state
  - Adds JWT tokens through Axios interceptors

---

## Project Structure

```text
vehicle-status-management-system/
│
├── src/
│   └── VehicleStatusSystem/
│       ├── Controllers/
│       ├── DTOs/
│       ├── Exceptions/
│       ├── Interfaces/
│       ├── Models/
│       ├── Repositories/
│       ├── Services/
│       ├── Program.cs
│       ├── appsettings.json
│       └── VehicleStatusSystem.csproj
│
├── tests/
│   └── VehicleStatusSystem.Tests/
│       └── Services/
│           └── VehicleServiceTests.cs
│
├── frontend/
│   └── vehicle-status-frontend/
│       ├── src/
│       │   ├── components/
│       │   ├── views/
│       │   ├── router/
│       │   ├── services/
│       │   ├── App.vue
│       │   └── main.js
│       ├── package.json
│       └── vite.config.js
│
├── database/
├── .gitignore
├── README.md
└── VehicleStatusSystem.slnx
```

---

## Database Design

The system currently contains three main tables:

```text
Users

Vehicles
    |
    | 1
    |
    | N
VehicleStatusRecords
```

### Vehicles

Main fields:

```text
Id
PlateNumber
Model
Status
CreateTime
```

`PlateNumber` has a unique constraint.

### VehicleStatusRecords

Main fields:

```text
Id
VehicleId
Temperature
Speed
Battery
RecordTime
```

`VehicleId` is a foreign key referencing `Vehicles.Id`.

### Users

Main fields:

```text
Id
Username
PasswordHash
Role
CreatedAt
```

Passwords are stored as hashes rather than plain text.

---

## Main API Endpoints

### Authentication

```http
POST /api/auth/register
POST /api/auth/login
```

### Vehicles

```http
GET    /api/vehicles?keyword=BYD&status=Active
GET    /api/vehicles/paged?keyword=BYD&status=Active&page=1&pageSize=10
GET    /api/vehicles/{id}
POST   /api/vehicles
PUT    /api/vehicles/{id}
DELETE /api/vehicles/{id}
```

### Vehicle Status

```http
GET  /api/vehicles/{vehicleId}/status
POST /api/vehicles/{vehicleId}/status
```

### Dashboard

```http
GET /api/dashboard/summary
```

The summary returns `totalVehicles`, `activeVehicles`, `inactiveVehicles`, and `totalRecords`.

Protected endpoints use:

```http
Authorization: Bearer <token>
```

---

## Authentication Flow

```text
Login Page
    |
    v
POST /api/auth/login
    |
    v
AuthController
    |
    v
AuthService
    |
    +---- UserRepository
    |
    +---- PasswordHasher
    |
    v
JwtTokenService
    |
    v
JWT Token
    |
    v
localStorage
    |
    v
Axios Request Interceptor
    |
    v
Authorization: Bearer <token>
```

The backend validates:

- Token signature
- Issuer
- Audience
- Expiration
- User claims
- Role claims

---

## Testing

The project includes service-layer unit tests using **xUnit** and **Moq**.

Current tests focus on core `VehicleService` business behavior such as:

- Vehicle existence checks
- Delete behavior
- Preventing deletion when historical records exist
- Duplicate plate-number validation
- Update failure / conflict scenarios

Run the tests:

```bash
dotnet test
```

---

## Logging

The backend uses ASP.NET Core `ILogger`.

Examples:

```text
Information
- Vehicle created
- Vehicle updated
- Vehicle deleted

Warning
- Resource not found
- Business rule violation
- Authentication failure

Error
- Unhandled application exception
- Database or system failure
```

Global exception handling prevents internal exception details from being exposed directly to clients.

---

## Configuration

Sensitive values should not be committed to Git.

Development secrets such as:

```text
ConnectionStrings:DefaultConnection
Jwt:SecretKey
```

can be stored with **.NET User Secrets**.

Example:

```bash
dotnet user-secrets set \
"ConnectionStrings:DefaultConnection" \
"<YOUR_CONNECTION_STRING>"
```

```bash
dotnet user-secrets set \
"Jwt:SecretKey" \
"<YOUR_JWT_SECRET>"
```

Do not commit database passwords, JWT secret keys, or valid JWT tokens.

---

## How to Run

> Use a supported .NET 10 SDK, SQL Server, and a compatible Node.js version (see `frontend/vehicle-status-frontend/package.json` for the Node.js engine range).
>
> The frontend source imports `echarts`. Before a reproducible fresh install is possible, ensure `echarts` is declared in `package.json` and commit a generated `package-lock.json`. See **Dependency Maintenance** below.

### 1. Clone

```bash
git clone https://github.com/Conner555/vehicle-status-management-system.git
cd vehicle-status-management-system
```

### 2. Initialize SQL Server and configure the backend

Execute these scripts in SQL Server, in order:

```text
database/init.sql
database/seed_demo.sql  (optional fictional demo records)
```

Then configure `ConnectionStrings:DefaultConnection` and `Jwt:SecretKey` using .NET User Secrets or environment variables. The SQL scripts do not create a demo Admin account; set up an authorized administrator separately without committing passwords or credentials.

```bash
cd src/VehicleStatusSystem
dotnet restore
dotnet build
dotnet run
```


The development API currently uses a local address such as:

```text
http://localhost:5129
```

### 3. Run backend tests

From the repository root:

```bash
dotnet test
```

### 4. Install and run the frontend

Open another terminal:

```bash
cd frontend/vehicle-status-frontend
npm ci  # after package-lock.json has been committed; otherwise npm install
npm run dev
```

The Vite development server normally runs at:

```text
http://localhost:5173
```

### 5. Frontend-to-backend communication

The Axios instance is configured in:

```text
frontend/vehicle-status-frontend/src/services/api.js
```

For local development, make sure the backend CORS policy allows the frontend origin.

---

## Dependency Maintenance

From `frontend/vehicle-status-frontend`:

```bash
npm ls echarts vue vue-router
npm install echarts --save
npm run build
```

Check the installed Vue and Vue Router versions for compatibility before changing the existing `rc` version ranges or overrides. Commit both `package.json` and `package-lock.json` after generating a lockfile from a working installation. Do **not** commit `node_modules/` or Vite `dist/` output. For subsequent clean installations, use `npm ci`.

## Current Status

Implemented:

- [x] Vehicle CRUD
- [x] Vehicle status history
- [x] SQL Server persistence
- [x] Repository / Service architecture
- [x] Dependency Injection
- [x] Async database operations
- [x] DTO validation
- [x] Global exception handling
- [x] Structured logging
- [x] User Secrets
- [x] Password hashing
- [x] JWT authentication
- [x] User / Admin role authorization
- [x] xUnit + Moq service-layer unit tests
- [x] Vue 3 frontend
- [x] Element Plus management UI
- [x] Frontend GET / POST / PUT / DELETE integration
- [x] Vue component separation with Props / Emits
- [x] Axios instance + JWT interceptor
- [x] Vue Router
- [x] Authentication navigation guard
- [x] Keyword search and vehicle status filtering
- [x] Server-side vehicle pagination
- [x] Vehicle detail pages and status-history chart
- [x] Dashboard summary API and ECharts visualization
- [x] Demo database seed script

Planned improvements:

- [ ] Integration tests
- [ ] Docker
- [ ] CI/CD with GitHub Actions
- [ ] Document V2 UI with updated screenshots
- [ ] Improve reproducible frontend dependency installation
- [ ] Optional business modules such as rental orders and reviews

---

## Learning Goals

This project was developed to practice and understand:

- ASP.NET Core Web API architecture
- RESTful API design
- SQL Server integration
- ADO.NET asynchronous programming
- Controller / Service / Repository separation
- Dependency Injection
- DTO validation
- Authentication and authorization
- Global exception handling and logging
- Unit testing with xUnit and Moq
- Vue 3 component-based frontend development
- Props / Emits and component `v-model`
- Axios instances and interceptors
- Vue Router and navigation guards
- Frontend-backend integration
- Git / GitHub project management
