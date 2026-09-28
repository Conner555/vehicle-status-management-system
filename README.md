# Vehicle Status Management System

A backend vehicle status management system built with **ASP.NET Core Web API** and **SQL Server**.

The project follows a layered architecture using **Controller - Service - Repository**, and implements vehicle management, historical status records, DTO validation, global exception handling, logging, JWT authentication, and role-based authorization.

---

## Features

### Vehicle Management

- Create vehicle information
- Query all vehicles
- Query vehicle by ID
- Update vehicle information
- Delete vehicle information
- Prevent duplicate plate numbers
- Prevent deletion of vehicles with historical status records

### Vehicle Status Management

- Add vehicle status records
- Query vehicle status history
- Record temperature, speed, battery level and timestamp
- One-to-many relationship between vehicles and status records

### Validation & Exception Handling

- DTO validation with `DataAnnotations`
- Global exception handling middleware
- Standard HTTP status handling:

| Status Code | Scenario |
| --- | --- |
| `200` | Request succeeded |
| `201` | Resource created |
| `204` | Resource deleted |
| `400` | Invalid request parameters |
| `401` | Authentication failed |
| `403` | Insufficient permissions |
| `404` | Resource not found |
| `409` | Business rule conflict |
| `500` | Internal server error |

### Authentication & Authorization

- User registration
- User login
- Password hashing
- JWT authentication
- Role-based authorization
- `User` and `Admin` roles

Example permission model:

| Operation | User | Admin |
| --- | :---: | :---: |
| View vehicles | ✓ | ✓ |
| View vehicle status | ✓ | ✓ |
| Create vehicle | ✗ | ✓ |
| Update vehicle | ✗ | ✓ |
| Delete vehicle | ✗ | ✓ |

---

## Tech Stack

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
- **Git / GitHub**

---

## Architecture

The project uses a layered architecture:

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

Responsibilities of each layer:

- **Controller**
  - Receives HTTP requests
  - Performs model binding
  - Returns HTTP responses

- **Service**
  - Handles business logic
  - Performs business rule validation
  - Converts between DTOs and Models

- **Repository**
  - Handles database access
  - Executes SQL commands through ADO.NET

- **Middleware**
  - Handles global exceptions
  - Provides unified error responses

---

## Project Structure

```text
VehicleStatusSystem/
│
├── Controllers/
│   ├── AuthController.cs
│   ├── VehiclesController.cs
│   └── VehicleStatusController.cs
│
├── DTOs/
│   ├── AuthResponseDto.cs
│   ├── CreateVehicleDto.cs
│   ├── CreateVehicleStatusDto.cs
│   ├── LoginDto.cs
│   ├── RegisterDto.cs
│   ├── UpdateVehicleDto.cs
│   ├── VehicleDto.cs
│   └── VehicleStatusDto.cs
│
├── Exceptions/
│   ├── BusinessRuleException.cs
│   ├── GlobalExceptionMiddleware.cs
│   ├── InvalidCredentialsException.cs
│   └── ResourceNotFoundException.cs
│
├── Interfaces/
│   ├── IAuthService.cs
│   ├── ITokenService.cs
│   ├── IUserRepository.cs
│   ├── IVehicleRepository.cs
│   ├── IVehicleService.cs
│   ├── IVehicleStatusRepository.cs
│   └── IVehicleStatusService.cs
│
├── Models/
│   ├── User.cs
│   ├── Vehicle.cs
│   └── VehicleStatusRecord.cs
│
├── Repositories/
│   ├── UserRepository.cs
│   ├── VehicleRepository.cs
│   └── VehicleStatusRepository.cs
│
├── Services/
│   ├── AuthService.cs
│   ├── JwtTokenService.cs
│   ├── VehicleService.cs
│   └── VehicleStatusService.cs
│
├── Program.cs
├── appsettings.json
├── VehicleStatusSystem.csproj
└── README.md
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

Stores basic vehicle information.

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

Stores historical operating status for vehicles.

Main fields:

```text
Id
VehicleId
Temperature
Speed
Battery
RecordTime
```

`VehicleId` is a foreign key referencing:

```text
Vehicles.Id
```

### Users

Stores authentication information.

Main fields:

```text
Id
Username
PasswordHash
Role
CreatedAt
```

Passwords are not stored as plain text.

---

## Main API Endpoints

### Authentication

```http
POST /api/auth/register
POST /api/auth/login
```

### Vehicles

```http
GET    /api/vehicles
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

Protected endpoints require a JWT:

```http
Authorization: Bearer <token>
```

---

## Example Authentication Flow

```text
Register / Login
        |
        v
AuthController
        |
        v
AuthService
   /          \
  v            v
UserRepository PasswordHasher
        |
        v
JwtTokenService
        |
        v
JWT Token
```

After login, the client sends the token with subsequent requests:

```http
Authorization: Bearer <JWT_TOKEN>
```

The server validates:

- Token signature
- Issuer
- Audience
- Expiration time
- User claims
- Role claims

---

## Logging

The system uses ASP.NET Core `ILogger`.

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

Sensitive information is not stored directly in `appsettings.json`.

The project uses **.NET User Secrets** for development secrets such as:

```text
ConnectionStrings:DefaultConnection
Jwt:SecretKey
```

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

Do not commit real database passwords, JWT secret keys, or valid JWT tokens to Git.

---

## OpenAPI

The project exposes an OpenAPI document in Development mode.

After running the application:

```text
http://localhost:<port>/openapi/v1.json
```

Swagger UI is not currently included.

---

## How to Run

### 1. Clone the repository

```bash
git clone <repository-url>
cd VehicleStatusSystem
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Configure SQL Server

Create the database:

```text
VehicleStatusDb
```

and configure the database connection string through User Secrets.

### 4. Configure JWT Secret

```bash
dotnet user-secrets set \
"Jwt:SecretKey" \
"<YOUR_JWT_SECRET>"
```

### 5. Build

```bash
dotnet build
```

### 6. Run

```bash
dotnet run
```

The terminal will display the local API address, for example:

```text
http://localhost:5129
```

---

## Current Status

Implemented:

- [x] Vehicle CRUD
- [x] Vehicle status history
- [x] SQL Server persistence
- [x] Repository pattern
- [x] Dependency Injection
- [x] Async database operations
- [x] DTO validation
- [x] Global exception handling
- [x] Structured logging
- [x] User Secrets
- [x] Password hashing
- [x] JWT authentication
- [x] Role-based authorization
- [x] OpenAPI document

Planned improvements:

- [ ] Unit tests
- [ ] Integration tests
- [ ] Docker
- [ ] CI/CD with GitHub Actions
- [ ] Improved API documentation

---

## Learning Goals

This project was developed to practice and understand:

- ASP.NET Core Web API architecture
- RESTful API design
- SQL Server integration
- ADO.NET asynchronous programming
- Dependency Injection
- Repository and Service patterns
- Authentication and authorization
- Error handling and logging
- Secure configuration management