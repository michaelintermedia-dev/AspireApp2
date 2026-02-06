# Authentication Setup - Installation Guide

## 1. Install Required Dependencies

Run this command in your project root (`my-mobile-test1`):

```bash
npx expo install expo-secure-store
```

or if you're using npm:

```bash
npm install expo-secure-store
```

or yarn:

```bash
yarn add expo-secure-store
```

## 2. Configuration Complete! ✅

All authentication files have been created:

### Services
- ✅ `auth/services/TokenService.ts` - Secure JWT storage
- ✅ `auth/services/AuthService.ts` - Authentication API calls
- ✅ `auth/services/AuthenticatedApiClient.ts` - Authenticated HTTP client

### Context & Hooks
- ✅ `auth/context/AuthContext.tsx` - React Context for auth state
- ✅ `auth/hooks/useAuth.ts` - Custom hook for easy access

### Screens
- ✅ `auth/screens/LoginScreen.tsx` - Login UI
- ✅ `auth/screens/RegisterScreen.tsx` - Registration UI
- ✅ `auth/screens/ForgotPasswordScreen.tsx` - Password reset UI

### Routes
- ✅ `app/auth/login.tsx` - Login route
- ✅ `app/auth/register.tsx` - Register route
- ✅ `app/auth/forgot-password.tsx` - Forgot password route

### Components
- ✅ `components/ProtectedRoute.tsx` - Route protection wrapper

### Examples
- ✅ `api/devices.ts` - Example authenticated API calls
- ✅ `auth/README.md` - Full usage documentation

## 3. Next Steps

### A. Update Your Existing App Entry Point

The `AuthProvider` has been added to `app/_layout.tsx`.

### B. Protect Your Routes

Wrap any protected screens with `ProtectedRoute`:

```typescript
import { ProtectedRoute } from '../../components/ProtectedRoute';

export default function MyScreen() {
  return (
    <ProtectedRoute>
      <View>
        {/* Your screen content */}
      </View>
    </ProtectedRoute>
  );
}
```

### C. Use Authentication in Your Components

```typescript
import { useAuth } from '../auth/hooks/useAuth';

function MyComponent() {
  const { user, isAuthenticated, logout } = useAuth();

  return (
    <View>
      {isAuthenticated && <Text>Welcome, {user?.email}!</Text>}
      <Button title="Logout" onPress={logout} />
    </View>
  );
}
```

### D. Make Authenticated API Calls

Replace your existing API calls with `AuthenticatedApiClient`:

```typescript
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';

// GET
const data = await AuthenticatedApiClient.get('/api/endpoint');

// POST
const result = await AuthenticatedApiClient.post('/api/endpoint', { data });

// PUT
const updated = await AuthenticatedApiClient.put('/api/endpoint', { data });

// DELETE
await AuthenticatedApiClient.delete('/api/endpoint');
```

## 4. Testing the Auth Flow

1. **Start your app:**
   ```bash
   npx expo start
   ```

2. **Test the flow:**
   - Open app → Should show login screen
   - Tap "Sign Up" → Register a new account
   - After registration → Auto-login and redirect to main app
   - Logout → Return to login screen
   - Login again → Should work with stored credentials

3. **Test token refresh:**
   - Login and use the app
   - Wait for token to expire (1 hour by default)
   - Make an API call → Token should auto-refresh
   - Continue using the app seamlessly

## 5. Security Features

✅ **Secure Storage**: Tokens stored in SecureStore on mobile, localStorage on web
✅ **Auto Refresh**: Expired tokens automatically refreshed
✅ **401 Handling**: Automatic retry on authentication errors
✅ **Token Validation**: JWT expiration checked before requests
✅ **Concurrent Safety**: Multiple requests won't trigger multiple refreshes

## 6. API Endpoints Used

Make sure your backend has these endpoints ready:
- `POST /auth/register` - User registration
- `POST /auth/login` - User login
- `POST /auth/refresh` - Token refresh
- `POST /auth/logout` - User logout
- `POST /auth/forgot-password` - Request password reset
- `POST /auth/reset-password` - Reset password with token

## 7. Customization

### Change Token Expiration
Update in your backend `AuthService.cs`:
```csharp
expires: DateTime.UtcNow.AddHours(24) // Change to 24 hours
```

### Customize UI
Edit the screen files in `auth/screens/` to match your design.

### Add More Auth Features
- Email verification
- Social login (Google, Apple, etc.)
- Biometric authentication
- Multi-factor authentication

## 8. Troubleshooting

### "Module not found: expo-secure-store"
Run: `npx expo install expo-secure-store`

### "Cannot read property 'user' of undefined"
Make sure `AuthProvider` is wrapping your app in `_layout.tsx`

### API calls returning 401
Check that:
1. Backend is running
2. JWT secret matches between app and backend
3. Token is not expired
4. User is logged in

### Navigation not working
Make sure you have `expo-router` properly set up.

## 9. Migration Checklist

- [ ] Install `expo-secure-store`
- [ ] Verify `AuthProvider` is in `_layout.tsx`
- [ ] Test login flow
- [ ] Test registration flow
- [ ] Test forgot password flow
- [ ] Wrap protected screens with `ProtectedRoute`
- [ ] Replace API calls with `AuthenticatedApiClient`
- [ ] Test token refresh (wait 1 hour or change expiration)
- [ ] Test logout and re-login

## Need Help?

Check `auth/README.md` for detailed API usage examples and best practices.
