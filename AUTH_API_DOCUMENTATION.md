# Authentication API Documentation - Enhanced Version

## Overview

This project implements a comprehensive authentication system with password management using JWT (JSON Web Tokens) and bcrypt password hashing, following Clean Architecture principles with Domain, Application, Infrastructure, and API layers.

## Features

- **User Registration**: Create new user accounts with bcrypt password hashing
- **User Login**: Authenticate users and return JWT tokens
- **JWT Bearer Authentication**: Secure API endpoints with JWT tokens
- **Password Reset**: Send password reset tokens and reset with validation
- **Change Password**: Authenticated users can change their password
- **Role-Based Access Control**: Support for Admin, Manager, Annotator, and Reviewer roles
- **Password Security**: Bcrypt hashing with configurable work factor
- **Secure Token Generation**: URL-safe Base64 random tokens for password reset
- **Async/Await Pattern**: Fully asynchronous API implementation

## Architecture Layers

### Domain Layer (`DataLabeling.Domain`)

**Models**:

- `User`: User entity with password hash and role
- `PasswordResetToken`: Tracks password reset tokens with expiration

**DTOs**:

- `LoginRequest`: Username and password
- `RegisterRequest`: Username, email, password, and role
- `ForgotPasswordRequest`: Email or username for reset
- `ResetPasswordRequest`: User ID, reset token, and new password
- `ChangePasswordRequest`: Current and new password
- `AuthResponse`: Standardized API response
- `AuthTokenResponse`: Access token, refresh token, expiration, user info
- `UserDto`: User information without sensitive data
- `UserRole`: Role constants

### Application Layer (`DataLabeling.Application`)

**Interfaces**:

- `IPasswordHasher`: Password hashing/verification
- `ITokenProvider`: JWT token operations
- `IAuthService`: Main authentication service (6 methods)
- `IPasswordResetService`: Password reset operations (4 methods)

### Infrastructure Layer (`DataLabeling.Infrastructure`)

**Services**:

- `BcryptPasswordHasher`: Bcrypt-based hashing (work factor 12)
- `JwtTokenProvider`: JWT token generation/validation
- `AuthService`: Orchestrates all auth operations
- `PasswordResetService`: Password reset/change logic

### API Layer (`DataLabeling.API`)

- **AuthController**: 6 RESTful endpoints
- **ServiceCollectionExtensions**: DI configuration
- **JWT Middleware**: Token validation

## API Endpoints

### 1. Login

**Endpoint**: `POST /api/auth/login`

**Request**:

```json
{
  "username": "john_doe",
  "password": "password123"
}
```

**Success Response**:

```json
{
  "isSuccess": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "base64_random_string",
    "expiresIn": 900,
    "user": {
      "id": 1,
      "username": "john_doe",
      "email": "john@example.com",
      "role": "Annotator",
      "status": "Active"
    }
  }
}
```

### 2. Register

**Endpoint**: `POST /api/auth/register`

**Request**:

```json
{
  "username": "jane_doe",
  "email": "jane@example.com",
  "password": "SecurePass123!",
  "role": "Manager"
}
```

**Validation**:

- Username: Required, unique
- Email: Required, unique
- Password: Minimum 6 characters
- Role: Optional (defaults to "Annotator")

### 3. Forgot Password

**Endpoint**: `POST /api/auth/forgot-password`

**Request**:

```json
{
  "emailOrUsername": "john_doe"
}
```

**Response** (Always success for security):

```json
{
  "isSuccess": true,
  "message": "If an account exists, a password reset email will be sent"
}
```

**Backend**:

- Generates secure random token
- Bcrypt-hashes token
- Stores with 30-minute expiration
- Invalidates previous tokens
- (Production) Sends email with reset link

### 4. Reset Password

**Endpoint**: `POST /api/auth/reset-password`

**Request**:

```json
{
  "userId": 1,
  "resetToken": "token_from_email",
  "newPassword": "NewPass123!",
  "confirmPassword": "NewPass123!"
}
```

**Validation**:

- Token must be valid and not expired
- Passwords must match
- Minimum 6 characters

### 5. Change Password (Requires Auth)

**Endpoint**: `POST /api/auth/change-password`

**Headers**:

```
Authorization: Bearer <access_token>
```

**Request**:

```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass456!",
  "confirmPassword": "NewPass456!"
}
```

**Validation**:

- User authenticated via token
- Current password correct
- New password different from current
- Passwords match

### 6. Refresh Token

**Endpoint**: `POST /api/auth/refresh`

**Request**:

```json
{
  "refreshToken": "refresh_token_from_login"
}
```

Note: Currently a stub, needs database storage for production

## Database Schema

### PasswordResetToken Table

```sql
PasswordResetTokens:
- Id (long) - Primary key
- UserId (long) - FK to User
- Token (string) - Bcrypt-hashed token
- ExpiryTime (datetime) - 30 min expiration
- IsUsed (bool) - One-time use flag
- CreatedAt (datetime) - Creation timestamp
```

## Security Implementation

- `POST /api/auth/register`: User registration
- `POST /api/auth/refresh`: Token refresh (stub)
- **ServiceCollectionExtensions**: Dependency injection configuration
- **JWT Middleware**: Token validation and authorization

## Security Implementation

### Password Hashing

- **Algorithm**: Bcrypt with work factor 12
- **Iterations**: 2^12 = 4,096 iterations
- **Processing Time**: ~100ms per hash (adjustable)
- **Salt**: Automatically generated and included
- **Security**: Password never stored, hash is irreversible

### JWT Tokens

- **Algorithm**: HMAC-SHA256
- **Claims**: User ID, username, role
- **Signature**: Verified on every request
- **Expiration**: 15 minutes (configurable)
- **Storage**: No sensitive data in token

### Password Reset Security

- **Token Format**: 32-byte random, Base64-encoded, URL-safe
- **Token Storage**: Bcrypt-hashed in database
- **One-Time Use**: Token marked as used after reset
- **Expiration**: 30 minutes
- **Privacy**: Doesn't reveal if user exists (ForgotPassword always succeeds)

### HTTPS Requirement

All authentication must use HTTPS in production

### Configuration

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-chars-long",
    "Issuer": "DataLabelingAPI",
    "Audience": "DataLabelingClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## Configuration & Setup

### Installation

1. Build: `dotnet build`
2. Database migrations: `dotnet ef database update`
3. Run: `dotnet run`

### Required NuGet Packages

- BCrypt.Net-Next (4.0.3)
- System.IdentityModel.Tokens.Jwt (7.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.16)

### Dependency Injection

All services automatically registered in Program.cs:

```csharp
services.AddAuthenticationServices(configuration);
```

## Usage Examples

### cURL - Complete Flow

Register:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPass123"
  }'
```

Login:

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "TestPass123"
  }'
```

Change Password (with token):

```bash
curl -X POST http://localhost:5000/api/auth/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "currentPassword": "TestPass123",
    "newPassword": "NewPass456",
    "confirmPassword": "NewPass456"
  }'
```

### C# Usage

```csharp
// Inject IAuthService
private readonly IAuthService _authService;

// Login
var loginReq = new LoginRequest { Username = "user", Password = "pass" };
var result = await _authService.LoginAsync(loginReq);

// Change password
var changeReq = new ChangePasswordRequest { ... };
var result = await _authService.ChangePasswordAsync(userId, changeReq);
```

### Role-Based Authorization

```csharp
[Authorize(Roles = "Manager,Admin")]
[HttpPost("assign-work")]
public async Task<IActionResult> AssignWork(AssignmentRequest request)
{
    // Only managers and admins
}

[Authorize(Roles = "Admin")]
[HttpDelete("users/{id}")]
public async Task<IActionResult> DeleteUser(long id)
{
    // Only admins
}
```

## Error Responses

All endpoints return consistent format:

```json
{
  "isSuccess": false,
  "message": "Error description"
}
```

**Common Errors**:

- "Username and password are required"
- "Invalid username or password"
- "User account is inactive"
- "Username already exists"
- "Email already exists"
- "Password must be at least 6 characters long"
- "Invalid or expired reset token"
- "Current password is incorrect"
- "New passwords do not match"

## Testing Guide

### Manual Testing Checklist

- [ ] Register user with valid data
- [ ] Register with duplicate username
- [ ] Register with invalid email
- [ ] Register with short password
- [ ] Login with correct credentials
- [ ] Login with wrong password
- [ ] Forgot password request
- [ ] Reset password with valid token
- [ ] Reset password with expired token
- [ ] Change password (authenticated)
- [ ] Verify JWT token validation
- [ ] Test role-based access

### Test Data

```sql
-- Insert test user
INSERT INTO users (username, email, password_hash, role, status, created_at)
VALUES ('testuser', 'test@example.com',
  '$2a$12$Hash...', 'Annotator', 'Active', GETUTCDATE())
```

## Logging

Events logged with ILogger:

- User registration
- Login attempts
- Password reset requests
- Password changes
- Authorization failures
- Token validation errors

## Future Enhancements

- [ ] Email service integration
- [ ] Two-factor authentication (2FA)
- [ ] Account lockout mechanism
- [ ] OAuth/OpenID Connect
- [ ] Token blacklisting
- [ ] Session management
- [ ] Audit trail with IP logging
- [ ] Password strength requirements
- [ ] Email verification
- [ ] Account recovery options

## Troubleshooting

**Issue**: Token expired
**Solution**: Use refresh token to get new access token

**Issue**: Unauthorized (401)
**Solution**: Include Authorization header: `Bearer {token}`

**Issue**: Forbidden (403)
**Solution**: Verify user has required role

**Issue**: Password reset fails
**Solution**: Check token hasn't expired (30 min), verify user ID matches token

## Support

For issues or questions, check the logs and verify:

1. JWT Key is configured correctly
2. Database connection is valid
3. User exists and is active
4. Token is not expired
5. Authorization header format is correct

## Production Checklist

- [ ] Change JWT Key to secure random 32+ character string
- [ ] Enable HTTPS only
- [ ] Set strong database password
- [ ] Implement email service for password reset
- [ ] Configure logging and monitoring
- [ ] Set up rate limiting on auth endpoints
- [ ] Enable CORS for specific domains only
- [ ] Implement token blacklist for logout
- [ ] Set up key rotation policy
- [ ] Test all error scenarios
- [ ] Load test authentication endpoints
