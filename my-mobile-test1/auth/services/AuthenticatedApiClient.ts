import { getApiUrl } from '../../api/client';
import { TokenService } from './TokenService';
import { AuthService } from './AuthService';

interface RequestOptions extends RequestInit {
  skipAuth?: boolean;
  skipRetry?: boolean;
}

export class AuthenticatedApiClient {
  private static isRefreshing = false;
  private static refreshPromise: Promise<void> | null = null;

  /**
   * Make an authenticated API request
   * Automatically adds JWT and handles token refresh
   */
  static async fetch<T>(
    endpoint: string,
    options: RequestOptions = {}
  ): Promise<T> {
    const { skipAuth = false, skipRetry = false, ...fetchOptions } = options;

    // Build headers
    const headers = new Headers(fetchOptions.headers);
    
    if (!skipAuth) {
      const token = await TokenService.getToken();
      
      if (!token) {
        throw new Error('Not authenticated');
      }

      // Check if token is expired and refresh if needed
      if (TokenService.isTokenExpired(token)) {
        await this.handleTokenRefresh();
        const newToken = await TokenService.getToken();
        if (newToken) {
          headers.set('Authorization', `Bearer ${newToken}`);
        }
      } else {
        headers.set('Authorization', `Bearer ${token}`);
      }
    }

    if (!headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json');
    }
    headers.set('Accept', 'application/json');

    // Make the request
    const url = getApiUrl(endpoint);
    console.log(`[AuthenticatedAPI] ${fetchOptions.method || 'GET'} ${url}`);

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 30000);

    try {
      const response = await fetch(url, {
        ...fetchOptions,
        headers,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      console.log(`[AuthenticatedAPI] Response: ${response.status}`);

      // Handle 401 Unauthorized - token might be invalid
      if (response.status === 401 && !skipAuth && !skipRetry) {
        console.log('[AuthenticatedAPI] 401 Unauthorized, attempting token refresh');
        
        try {
          await this.handleTokenRefresh();
          // Retry the request once with new token
          return await this.fetch<T>(endpoint, { ...options, skipRetry: true });
        } catch (refreshError) {
          console.error('[AuthenticatedAPI] Token refresh failed:', refreshError);
          // Clear tokens and throw auth error
          await TokenService.clearTokens();
          throw new Error('Authentication failed. Please login again.');
        }
      }

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`API Error: ${response.status} - ${errorText}`);
      }

      // Parse response
      const contentType = response.headers.get('content-type');
      if (contentType?.includes('application/json')) {
        return await response.json();
      } else {
        return (await response.text()) as T;
      }
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          throw new Error(`Request timeout for ${endpoint}`);
        }
        throw error;
      }
      throw new Error('Unknown API error');
    }
  }

  /**
   * Handle token refresh with concurrency control
   */
  private static async handleTokenRefresh(): Promise<void> {
    // If already refreshing, wait for that promise
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise;
    }

    // Start refresh process
    this.isRefreshing = true;
    this.refreshPromise = (async () => {
      try {
        console.log('[AuthenticatedAPI] Refreshing token...');
        await AuthService.refreshToken();
        console.log('[AuthenticatedAPI] Token refreshed successfully');
      } finally {
        this.isRefreshing = false;
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  /**
   * Convenience methods
   */
  static async get<T>(endpoint: string, options?: RequestOptions): Promise<T> {
    return this.fetch<T>(endpoint, { ...options, method: 'GET' });
  }

  static async post<T>(endpoint: string, data: unknown, options?: RequestOptions): Promise<T> {
    return this.fetch<T>(endpoint, {
      ...options,
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  static async put<T>(endpoint: string, data: unknown, options?: RequestOptions): Promise<T> {
    return this.fetch<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: JSON.stringify(data),
    });
  }

  static async delete<T>(endpoint: string, options?: RequestOptions): Promise<T> {
    return this.fetch<T>(endpoint, { ...options, method: 'DELETE' });
  }
}
