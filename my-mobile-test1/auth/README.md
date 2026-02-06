# Using Authenticated API Client

## Overview
All authenticated API calls should go through the `AuthenticatedApiClient` to ensure JWT tokens are automatically included and refreshed when needed.

## Basic Usage

### Import the client
```typescript
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';
```

### Making API Calls

#### GET Request
```typescript
const getUserProfile = async () => {
  try {
    const profile = await AuthenticatedApiClient.get('/api/user/profile');
    return profile;
  } catch (error) {
    console.error('Failed to get profile:', error);
    throw error;
  }
};
```

#### POST Request
```typescript
const createRecording = async (recordingData) => {
  try {
    const result = await AuthenticatedApiClient.post('/api/recordings', recordingData);
    return result;
  } catch (error) {
    console.error('Failed to create recording:', error);
    throw error;
  }
};
```

#### PUT Request
```typescript
const updateProfile = async (userId, data) => {
  try {
    const result = await AuthenticatedApiClient.put(`/api/user/${userId}`, data);
    return result;
  } catch (error) {
    console.error('Failed to update profile:', error);
    throw error;
  }
};
```

#### DELETE Request
```typescript
const deleteRecording = async (recordingId) => {
  try {
    await AuthenticatedApiClient.delete(`/api/recordings/${recordingId}`);
  } catch (error) {
    console.error('Failed to delete recording:', error);
    throw error;
  }
};
```

## Example: Converting Existing Device Registration

### Before (using apiPost)
```typescript
import { apiPost } from '../api/client';

const registerDevice = async (token: string) => {
  await apiPost('/devices/register', {
    token,
    platform: Platform.OS,
    deviceModel: Device.modelName,
  });
};
```

### After (using AuthenticatedApiClient)
```typescript
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';

const registerDevice = async (token: string) => {
  await AuthenticatedApiClient.post('/devices/register', {
    token,
    platform: Platform.OS,
    deviceModel: Device.modelName,
  });
};
```

## Features

### Automatic JWT Injection
The client automatically adds the JWT token to every request:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Automatic Token Refresh
If the token is expired or a 401 error is received, the client will:
1. Automatically refresh the token using the refresh token
2. Retry the original request with the new token
3. If refresh fails, clear tokens and throw an authentication error

### Concurrency-Safe Refresh
Multiple simultaneous requests won't trigger multiple refresh calls. If a refresh is already in progress, other requests will wait for it to complete.

### Skip Authentication (for public endpoints)
```typescript
const result = await AuthenticatedApiClient.get('/api/public/data', {
  skipAuth: true
});
```

## Error Handling

The client will throw errors in these cases:
- No token available (user not logged in)
- Network errors
- API errors (non-2xx status codes)
- Token refresh failed

Handle errors appropriately:
```typescript
try {
  const data = await AuthenticatedApiClient.get('/api/protected-resource');
  // Use data
} catch (error) {
  if (error.message.includes('Not authenticated')) {
    // Redirect to login
    router.push('/auth/login');
  } else if (error.message.includes('Authentication failed')) {
    // Token refresh failed, redirect to login
    router.push('/auth/login');
  } else {
    // Other error
    Alert.alert('Error', error.message);
  }
}
```

## Using with Auth Context

The `useAuth` hook provides the authentication state:

```typescript
import { useAuth } from '../auth/hooks/useAuth';
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';

function MyComponent() {
  const { isAuthenticated, user, logout } = useAuth();

  const fetchData = async () => {
    if (!isAuthenticated) {
      router.push('/auth/login');
      return;
    }

    try {
      const data = await AuthenticatedApiClient.get('/api/my-data');
      // Use data
    } catch (error) {
      if (error.message.includes('Authentication failed')) {
        await logout();
        router.push('/auth/login');
      }
    }
  };

  return (
    <View>
      {isAuthenticated && (
        <Text>Welcome, {user?.email}!</Text>
      )}
    </View>
  );
}
```

## Protected Routes Example

Create a wrapper component for protected screens:

```typescript
// components/ProtectedRoute.tsx
import { useAuth } from '../auth/hooks/useAuth';
import { useEffect } from 'react';
import { router } from 'expo-router';
import { ActivityIndicator, View } from 'react-native';

export function ProtectedRoute({ children }) {
  const { isAuthenticated, isLoading } = useAuth();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace('/auth/login');
    }
  }, [isAuthenticated, isLoading]);

  if (isLoading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return children;
}
```

Then use it in your screens:
```typescript
import { ProtectedRoute } from '../../components/ProtectedRoute';

export default function MyProtectedScreen() {
  return (
    <ProtectedRoute>
      <View>
        {/* Your protected content */}
      </View>
    </ProtectedRoute>
  );
}
```
