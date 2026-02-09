import React, { createContext, useState, useEffect, ReactNode } from 'react';
import { AuthService, LoginRequest, RegisterRequest } from '../services/AuthService';
import { TokenService } from '../services/TokenService';
import { registerForPushNotifications } from '../../api/notifications';
import { registerDeviceWithBackend } from '../../api/devices';

interface User {
  id: number;
  email: string;
}

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  forgotPassword: (email: string) => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

async function registerDevice() {
  try {
    const fcmToken = await registerForPushNotifications();
    if (fcmToken) {
      await registerDeviceWithBackend(fcmToken);
      console.log('[AuthContext] Device registered for push notifications');
    }
  } catch (error) {
    console.error('[AuthContext] Device registration failed:', error);
  }
}

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check authentication status on mount
  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const token = await TokenService.getToken();
      
      if (token && !TokenService.isTokenExpired(token)) {
        const userId = await TokenService.getUserId();
        const decoded = TokenService.decodeToken(token);
        
        if (userId && decoded) {
          setUser({
            id: userId,
            email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email,
          });

          // Register device when user is already logged in
          registerDevice();
        }
      } else {
        // Token expired or doesn't exist
        await TokenService.clearTokens();
        setUser(null);
      }
    } catch (error) {
      console.error('[AuthContext] Check auth error:', error);
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (request: LoginRequest) => {
    try {
      const response = await AuthService.login(request);
      const decoded = TokenService.decodeToken(response.token);
      
      setUser({
        id: response.userId,
        email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email,
      });

      // Register device right after login
      registerDevice();
    } catch (error) {
      console.error('[AuthContext] Login error:', error);
      throw error;
    }
  };

  const register = async (request: RegisterRequest) => {
    try {
      const response = await AuthService.register(request);
      const decoded = TokenService.decodeToken(response.token);
      const userId = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      
      setUser({
        id: parseInt(userId, 10),
        email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded.email,
      });

      // Register device right after registration
      registerDevice();
    } catch (error) {
      console.error('[AuthContext] Register error:', error);
      throw error;
    }
  };

  const logout = async () => {
    try {
      await AuthService.logout();
    } catch (error) {
      console.error('[AuthContext] Logout error:', error);
    } finally {
      setUser(null);
    }
  };

  const forgotPassword = async (email: string) => {
    await AuthService.forgotPassword(email);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: user !== null,
        login,
        register,
        logout,
        forgotPassword,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
