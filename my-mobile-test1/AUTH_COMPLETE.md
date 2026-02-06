# 🔐 Authentication System - Complete Implementation

## ✅ What's Been Created

### 📁 Folder Structure
```
my-mobile-test1/
├── auth/
│   ├── services/
│   │   ├── TokenService.ts                    ✅ JWT token management
│   │   ├── AuthService.ts                     ✅ Auth API calls
│   │   └── AuthenticatedApiClient.ts          ✅ Authenticated HTTP client
│   ├── context/
│   │   └── AuthContext.tsx                    ✅ React Context for auth state
│   ├── hooks/
│   │   └── useAuth.ts                         ✅ Custom hook
│   ├── screens/
│   │   ├── LoginScreen.tsx                    ✅ Login UI
│   │   ├── RegisterScreen.tsx                 ✅ Registration UI
│   │   └── ForgotPasswordScreen.tsx           ✅ Password reset UI
│   ├── index.ts                               ✅ Barrel exports
│   └── README.md                              ✅ Usage documentation
├── app/
│   ├── _layout.tsx                            ✅ Updated with AuthProvider
│   ├── auth/
│   │   ├── login.tsx                          ✅ Login route
│   │   ├── register.tsx                       ✅ Register route
│   │   └── forgot-password.tsx                ✅ Forgot password route
│   └── (tabs)/
│       └── profile.tsx                        ✅ Example protected screen
├── components/
│   └── ProtectedRoute.tsx                     ✅ Route protection wrapper
├── api/
│   └── devices.ts                             ✅ Example authenticated API
└── AUTHENTICATION_SETUP.md                     ✅ Installation guide
```

## 🚀 Quick Start

### 1. Install Dependencies
```bash
cd my-mobile-test1
npx expo install expo-secure-store
```

### 2. That's It!
Everything else is already configured! The `AuthProvider` has been added to your root layout.

## 🎯 Key Features

### 🔒 Security
- **Secure Token Storage**: Uses `expo-secure-store` on mobile, localStorage on web
- **JWT Management**: Automatic token refresh before expiration
- **401 Handling**: Auto-retry with refreshed token on auth errors
- **Token Validation**: Checks expiration before making requests

### 🔄 Automatic Token Refresh
```typescript
// Your code stays simple
const data = await AuthenticatedApiClient.get('/api/data');
// Token refresh happens automatically if needed!
```

### 🛡️ Protected Routes
```typescript
import { ProtectedRoute } from '../../components/ProtectedRoute';

export default function MyScreen() {
  return (
    <ProtectedRoute>
      {/* Only authenticated users can access this */}
    </ProtectedRoute>
  );
}
```

### 🎨 Complete Auth UI
- Modern, clean design
- Form validation
- Loading states
- Error handling
- Navigation flow

## 📖 How to Use

### Login
```typescript
import { useAuth } from '../auth/hooks/useAuth';

function LoginExample() {
  const { login } = useAuth();

  const handleLogin = async () => {
    await login({
      email: 'user@example.com',
      password: 'SecurePassword123!',
      platform: Platform.OS,
      deviceToken: 'device-123',
    });
  };
}
```

### Check Auth Status
```typescript
import { useAuth } from '../auth/hooks/useAuth';

function MyComponent() {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingSpinner />;
  if (!isAuthenticated) return <LoginPrompt />;

  return <Text>Welcome, {user?.email}!</Text>;
}
```

### Make Authenticated API Calls
```typescript
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';

// GET
const profile = await AuthenticatedApiClient.get('/api/user/profile');

// POST
const recording = await AuthenticatedApiClient.post('/api/recordings', {
  title: 'My Recording',
  duration: 120,
});

// PUT
const updated = await AuthenticatedApiClient.put('/api/user/profile', {
  firstName: 'John',
  lastName: 'Doe',
});

// DELETE
await AuthenticatedApiClient.delete('/api/recordings/123');
```

### Logout
```typescript
import { useAuth } from '../auth/hooks/useAuth';

function LogoutButton() {
  const { logout } = useAuth();

  return (
    <Button 
      title="Logout" 
      onPress={() => logout()} 
    />
  );
}
```

## 🔄 Migration Guide

### Replace Unauthenticated Calls

**Before:**
```typescript
import { apiPost } from '../api/client';
await apiPost('/devices/register', { token });
```

**After:**
```typescript
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';
await AuthenticatedApiClient.post('/devices/register', { token });
```

### Add Protection to Screens

**Before:**
```typescript
export default function MyScreen() {
  return <View>...</View>;
}
```

**After:**
```typescript
import { ProtectedRoute } from '../../components/ProtectedRoute';

export default function MyScreen() {
  return (
    <ProtectedRoute>
      <View>...</View>
    </ProtectedRoute>
  );
}
```

## 🧪 Testing Checklist

- [ ] **Registration Flow**
  - Navigate to register screen
  - Fill in email, password
  - Submit form
  - Verify auto-login after registration
  - Check token is stored

- [ ] **Login Flow**
  - Navigate to login screen
  - Enter credentials
  - Submit form
  - Verify redirect to main app
  - Check user state is set

- [ ] **Forgot Password**
  - Navigate to forgot password
  - Enter email
  - Verify success message
  - (Check email if backend configured)

- [ ] **Protected Routes**
  - Try to access protected screen while logged out
  - Verify redirect to login
  - Login and verify access granted

- [ ] **Token Refresh**
  - Login
  - Make an API call
  - Wait for token to expire (1 hour default)
  - Make another API call
  - Verify token is refreshed automatically
  - Verify request succeeds

- [ ] **Logout**
  - Click logout
  - Verify redirect to login
  - Verify tokens cleared
  - Try to access protected screen
  - Verify redirect to login

- [ ] **API Calls**
  - Make authenticated GET request
  - Make authenticated POST request
  - Verify JWT is included in headers
  - Verify requests succeed

## 🎨 Customization

### Change Theme Colors
Edit the `styles` in each screen file:
- `LoginScreen.tsx`
- `RegisterScreen.tsx`
- `ForgotPasswordScreen.tsx`

### Adjust Token Expiration
In your backend `AuthService.cs`:
```csharp
expires: DateTime.UtcNow.AddHours(24) // Change from 1 to 24 hours
```

### Add Custom Claims
In your backend `GenerateJwtToken`:
```csharp
new Claim("role", user.Role),
new Claim("subscription", user.SubscriptionTier),
```

Access in your app:
```typescript
const decoded = TokenService.decodeToken(token);
const role = decoded.role;
```

## 🔍 Architecture Overview

### Token Flow
```
1. User logs in
   ↓
2. Backend returns JWT + Refresh Token
   ↓
3. Tokens stored securely (SecureStore)
   ↓
4. API request made
   ↓
5. AuthenticatedApiClient checks token expiration
   ↓
6a. If expired → Refresh token → Retry request
6b. If valid → Add to Authorization header → Make request
   ↓
7. If 401 received → Refresh token → Retry once
   ↓
8. If refresh fails → Clear tokens → Redirect to login
```

### State Management
```
AuthProvider (Root)
    ↓
AuthContext (Global State)
    ↓
useAuth() hook (Components)
    ↓
Components access: user, isAuthenticated, login, logout, etc.
```

### API Client Flow
```
AuthenticatedApiClient.get('/api/endpoint')
    ↓
Check if token exists → No? → Throw error
    ↓
Check if token expired → Yes? → Refresh token
    ↓
Add Authorization header with Bearer token
    ↓
Make request
    ↓
401 received? → Refresh token → Retry once
    ↓
Success? → Return data
Failure? → Throw error
```

## 📚 API Endpoints Required

Make sure your backend has these endpoints:
```
POST /auth/register      - User registration
POST /auth/login         - User login
POST /auth/refresh       - Token refresh
POST /auth/logout        - User logout
POST /auth/forgot-password - Request password reset
POST /auth/reset-password  - Reset password with token
```

## 🐛 Troubleshooting

### "Module not found: expo-secure-store"
**Solution:** Run `npx expo install expo-secure-store`

### "useAuth must be used within an AuthProvider"
**Solution:** Make sure `AuthProvider` wraps your app in `_layout.tsx` ✅ (Already done!)

### API returns 401 even after login
**Check:**
- Backend is running on correct port
- JWT secret matches between app and backend
- Token is not expired (check expiration time)
- Authorization header format is correct

### Token not refreshing
**Check:**
- Refresh token is stored correctly
- `/auth/refresh` endpoint returns new tokens
- Token expiration is set correctly in backend

## 📱 Platform Support

- ✅ iOS
- ✅ Android  
- ✅ Web (uses localStorage instead of SecureStore)

## 🔐 Security Best Practices Implemented

1. ✅ Tokens stored securely (SecureStore on mobile)
2. ✅ HTTPS recommended for production (configure in backend)
3. ✅ Token expiration enforced
4. ✅ Refresh token rotation (backend generates new refresh token on each refresh)
5. ✅ Password validation (8+ chars, uppercase, lowercase, number, special char)
6. ✅ Email validation
7. ✅ Logout clears all tokens
8. ✅ 401 errors handled gracefully

## 📊 Example: Full Authentication Flow in Your App

```typescript
// 1. User opens app
//    → AuthProvider checks for stored token
//    → If valid, sets user state
//    → If invalid/expired, user stays logged out

// 2. User navigates to protected screen
//    → ProtectedRoute checks isAuthenticated
//    → If false, redirects to /auth/login

// 3. User logs in
//    → AuthService.login() called
//    → Backend returns tokens
//    → TokenService stores tokens
//    → AuthContext sets user state
//    → User redirected to main app

// 4. User makes API call
//    → AuthenticatedApiClient.get('/api/data')
//    → Gets token from storage
//    → Checks if expired → refreshes if needed
//    → Adds Authorization header
//    → Makes request
//    → Returns data

// 5. Token expires during usage
//    → Next API call detects expiration
//    → Automatically refreshes token
//    → Retries original request
//    → User doesn't notice anything

// 6. User logs out
//    → AuthService.logout() called
//    → Backend invalidates refresh token
//    → Local tokens cleared
//    → User state cleared
//    → Redirected to login
```

## 🎉 You're All Set!

Your authentication system is fully implemented and ready to use. Just install `expo-secure-store` and start building!

For detailed API usage examples, see `auth/README.md`.
