import { apiPost } from '../../api/client';
import { TokenService } from './TokenService';

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceToken?: string;
  platform?: string;
}

export interface RegisterResponse {
  message: string;
  token: string;
  refreshToken: string;
}

export interface LoginResponse {
  message: string;
  token: string;
  refreshToken: string;
  userId: number;
}

export interface RefreshResponse {
  token: string;
  refreshToken: string;
}

export class AuthService {
  /**
   * Register a new user
   */
  static async register(request: RegisterRequest): Promise<RegisterResponse> {
    const response = await apiPost<RegisterResponse>('/auth/register', request);
    
    // Store tokens after successful registration
    await TokenService.setTokens(response.token, response.refreshToken);
    
    return response;
  }

  /**
   * Login user
   */
  static async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await apiPost<LoginResponse>('/auth/login', request);
    
    // Store tokens and user ID after successful login
    await TokenService.setTokens(response.token, response.refreshToken, response.userId);
    
    return response;
  }

  /**
   * Refresh access token
   */
  static async refreshToken(): Promise<RefreshResponse> {
    const refreshToken = await TokenService.getRefreshToken();
    
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await apiPost<RefreshResponse>('/auth/refresh', {
      refreshToken,
    });

    // Update stored tokens
    await TokenService.setTokens(response.token, response.refreshToken);

    return response;
  }

  /**
   * Logout user
   */
  static async logout(): Promise<void> {
    try {
      const userId = await TokenService.getUserId();
      const refreshToken = await TokenService.getRefreshToken();

      if (userId && refreshToken) {
        await apiPost('/auth/logout', {
          userId,
          refreshToken,
        });
      }
    } catch (error) {
      console.error('[AuthService] Logout error:', error);
      // Continue with local logout even if API call fails
    } finally {
      // Always clear local tokens
      await TokenService.clearTokens();
    }
  }

  /**
   * Request password reset
   */
  static async forgotPassword(email: string): Promise<void> {
    await apiPost('/auth/forgot-password', { email });
  }

  /**
   * Reset password with token
   */
  static async resetPassword(token: string, newPassword: string): Promise<void> {
    await apiPost('/auth/reset-password', { token, newPassword });
  }
}
