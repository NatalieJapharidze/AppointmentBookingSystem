# Appointment Booking System

A REST API for appointment booking built with .NET 9, following Clean Architecture principles. The system allows managing service providers, booking appointments, handling conflicts, recurring appointments, and sending email notifications.

## 🏗️ Architecture

This project follows **Clean Architecture** with clear separation of concerns:

```
AppointmentBooking/
├── Domain/                 # Business entities, value objects, enums
│   ├── Entities/          # Core business entities
│   ├── ValueObjects/      # Domain value objects
│   ├── Enums/            # Domain enumerations
│   └── Common/           # Shared domain logic
├── Application/          # Use cases, commands, queries
│   ├── Appointments/     # Appointment-related operations
│   ├── Providers/        # Provider management
│   ├── Common/          # Shared application logic
│   └── Behaviors/       # Cross-cutting concerns
├── Infrastructure/       # External dependencies
│   ├── Persistence/     # Database context & configurations
│   ├── Services/        # External services (Email, DateTime)
│   └── BackgroundJobs/  # Background processing
└── WebApi/              # API controllers, filters, startup
```

## 🚀 Technologies

- **.NET 9** - Framework
- **PostgreSQL 16** - Database
- **Entity Framework Core** - ORM
- **MediatR** - CQRS pattern
- **FluentValidation** - Input validation
- **Docker & Docker Compose** - Containerization
- **Swagger** - API documentation

## 🛠️ Setup Instructions

### Prerequisites
- .NET 9 SDK
- Docker & Docker Compose
- PostgreSQL (if running locally)

### 1. Clone Repository
```bash
git clone <repository-url>
cd AppointmentBooking
```

### 2. Running with Docker (Recommended)
```bash
# Start the entire stack
docker-compose up -d

# View logs
docker-compose logs -f appointment_api
```

**Services:**
- **API**: http://localhost:5085
- **Swagger**: http://localhost:5085/swagger
- **PostgreSQL**: localhost:5400
- **Health Check**: http://localhost:5085/health

### 3. Running Locally

#### Database Setup
```bash
# Start only PostgreSQL
docker-compose up postgres -d
```

#### Run API
```bash
cd WebApi
dotnet restore
dotnet run
```

**Local URLs:**
- **HTTPS**: https://localhost:7022
- **HTTP**: http://localhost:5085
- **Swagger**: https://localhost:7022/swagger

## ⚙️ Configuration

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5400;Database=AppointmentBooking;Username=postgres;Password=password123"
  }
}
```

### Email Settings
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@yourapp.com",
    "FromName": "Appointment Booking System",
    "EnableEmailSending": true
  }
}
```

## 📋 Features

### 🏥 Service Provider Management
- Provider profiles with specialties
- Working hours configuration
- Break time management
- Holiday/vacation blocking

### 📅 Appointment Booking
- Search available time slots
- Book appointments with conflict detection
- Recurring appointments (weekly/monthly)
- Cancellation with reasons
- Rescheduling functionality

### 📧 Notifications
- Email confirmation on booking
- 24-hour reminders
- Cancellation notifications
- Provider notifications

### 🔒 Business Rules
- No double booking
- Minimum 24h advance booking
- Maximum 3 months ahead booking
- Business hours validation
- Duration constraints (15, 30, 45, 60 minutes)

## 🔌 API Endpoints

### Providers
```
GET    /api/providers                 # Get all providers
GET    /api/providers/{id}            # Get provider by ID
POST   /api/providers                 # Create provider
PUT    /api/providers/{id}            # Update provider
DELETE /api/providers/{id}            # Delete provider
```

### Appointments
```
GET    /api/appointments              # Get appointments
GET    /api/appointments/{id}         # Get appointment by ID
POST   /api/appointments              # Create appointment
PUT    /api/appointments/{id}         # Update appointment
DELETE /api/appointments/{id}         # Cancel appointment
```

### Available Slots
```
GET    /api/providers/{id}/available-slots?date={date}&duration={minutes}
```

## 🐳 Docker Commands

```bash
# Build and start all services
docker-compose up --build

# Stop all services
docker-compose down

# View service logs
docker-compose logs appointment_api
docker-compose logs postgres

# Rebuild API only
docker-compose up --build appointment_api

# Database shell access
docker exec -it appointment_booking_db psql -U postgres -d AppointmentBooking
```

## 🗄️ Database

The system automatically initializes the database schema on startup. The database includes:

- **service_providers** - Provider information and settings
- **working_hours** - Provider working schedules
- **appointments** - Appointment data with recurring support
- **blocked_times** - Provider unavailable periods
- **notification_logs** - Email notification tracking

## 🚦 Health Checks

- **API Health**: `/health`
- **Database Health**: Automatic with dependency checks

## 🔧 Development

### Adding Migrations
```bash
cd Infrastructure
dotnet ef migrations add MigrationName --startup-project ../WebApi
```

### Running Tests
```bash
dotnet test
```

## 📝 Business Logic Examples

### Conflict Detection
The system prevents double booking by checking:
- Overlapping time slots
- Provider availability
- Working hours constraints
- Blocked time periods

### Recurring Appointments
Supports:
- Weekly recurring patterns
- Monthly recurring patterns
- Exception handling for holidays
- Series modification capabilities

## 🎯 Key Design Patterns

- **CQRS** - Command Query Responsibility Segregation
- **Specification Pattern** - Complex business rule queries
- **Repository Pattern** - Data access abstraction
- **Domain Events** - Decoupled business logic
- **Value Objects** - Rich domain modeling

## 🛡️ Error Handling

Global exception handling with:
- Structured error responses
- Validation error details
- Logging integration
- User-friendly messages

## 📧 Email Integration

Automated email notifications for:
- Appointment confirmations
- 24-hour reminders
- Cancellation notices
- Provider alerts

Configure SMTP settings in `appsettings.json` or environment variables.

## 🚀 Production Deployment

1. Update connection strings
2. Configure email settings
3. Set `ASPNETCORE_ENVIRONMENT=Production`
4. Use production-grade PostgreSQL
5. Enable HTTPS
6. Configure proper logging

---