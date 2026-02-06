import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

const TOKEN_KEY = 'auth_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_ID_KEY = 'user_id';

export class TokenService {
  /**
   * Store tokens securely
   */
  static async setTokens(token: string, refreshToken: string, userId?: number): Promise<void> {
    try {
      if (Platform.OS === 'web') {
        // For web, use localStorage (less secure but functional)
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
        if (userId !== undefined) {
          localStorage.setItem(USER_ID_KEY, userId.toString());
        }
      } else {
        // For mobile, use SecureStore
        await SecureStore.setItemAsync(TOKEN_KEY, token);
        await SecureStore.setItemAsync(REFRESH_TOKEN_KEY, refreshToken);
        if (userId !== undefined) {
          await SecureStore.setItemAsync(USER_ID_KEY, userId.toString());
        }
      }
    } catch (error) {
      console.error('[TokenService] Error storing tokens:', error);
      throw new Error('Failed to store authentication tokens');
    }
  }

  /**
   * Get access token
   */
  static async getToken(): Promise<string | null> {
    try {
      if (Platform.OS === 'web') {
        return localStorage.getItem(TOKEN_KEY);
      } else {
        return await SecureStore.getItemAsync(TOKEN_KEY);
      }
    } catch (error) {
      console.error('[TokenService] Error getting token:', error);
      return null;
    }
  }

  /**
   * Get refresh token
   */
  static async getRefreshToken(): Promise<string | null> {
    try {
      if (Platform.OS === 'web') {
        return localStorage.getItem(REFRESH_TOKEN_KEY);
      } else {
        return await SecureStore.getItemAsync(REFRESH_TOKEN_KEY);
      }
    } catch (error) {
      console.error('[TokenService] Error getting refresh token:', error);
      return null;
    }
  }

  /**
   * Get user ID
   */
  static async getUserId(): Promise<number | null> {
    try {
      let userIdStr: string | null;
      if (Platform.OS === 'web') {
        userIdStr = localStorage.getItem(USER_ID_KEY);
      } else {
        userIdStr = await SecureStore.getItemAsync(USER_ID_KEY);
      }
      return userIdStr ? parseInt(userIdStr, 10) : null;
    } catch (error) {
      console.error('[TokenService] Error getting user ID:', error);
      return null;
    }
  }

  /**
   * Clear all tokens (logout)
   */
  static async clearTokens(): Promise<void> {
    try {
      if (Platform.OS === 'web') {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        localStorage.removeItem(USER_ID_KEY);
      } else {
        await SecureStore.deleteItemAsync(TOKEN_KEY);
        await SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY);
        await SecureStore.deleteItemAsync(USER_ID_KEY);
      }
    } catch (error) {
      console.error('[TokenService] Error clearing tokens:', error);
    }
  }

  /**
   * Check if user is authenticated
   */
  static async isAuthenticated(): Promise<boolean> {
    const token = await this.getToken();
    return token !== null;
  }

  /**
   * Decode JWT payload (without verification)
   */
  static decodeToken(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('[TokenService] Error decoding token:', error);
      return null;
    }
  }

  /**
   * Check if token is expired
   */
  static isTokenExpired(token: string): boolean {
    const decoded = this.decodeToken(token);
    if (!decoded || !decoded.exp) {
      return true;
    }
    const expirationTime = decoded.exp * 1000; // Convert to milliseconds
    return Date.now() >= expirationTime;
  }
}
