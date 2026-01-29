import { Platform } from 'react-native';

/**
 * Get the API base URL based on platform
 * - Web: uses localhost
 * - Mobile: uses EXPO_PUBLIC_API_HOST from .env
 */
export function getApiBaseUrl(): string {
  const host = Platform.OS === 'web' 
    ? 'localhost' 
    : process.env.EXPO_PUBLIC_API_HOST || '192.168.1.221';
  
  const port = process.env.EXPO_PUBLIC_API_PORT || '5132';
  
  console.log(`[API] Platform: ${Platform.OS}, Host: ${host}, Port: ${port}`);
  
  return `http://${host}:${port}`;
}

/**
 * Create full API URL
 * @param endpoint - API endpoint path (e.g., '/devices/register')
 */
export function getApiUrl(endpoint: string): string {
  const baseUrl = getApiBaseUrl();
  return `${baseUrl}${endpoint.startsWith('/') ? endpoint : '/' + endpoint}`;
}

/**
 * Generic fetch with error handling
 */
export async function apiFetch<T>(
  endpoint: string,
  options?: RequestInit
): Promise<T> {
  const url = getApiUrl(endpoint);
  
  console.log(`[API] ${options?.method || 'GET'} ${url}`);

  try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 30000); // 30s timeout

    const response = await fetch(url, {
      ...options,
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    console.log(`[API] Response: ${response.status} ${response.statusText}`);

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`);
    }

    // Handle empty responses
    const contentType = response.headers.get('content-type');
    if (contentType?.includes('application/json')) {
      const data = await response.json();
      console.log(`[API] Response body:`, data);
      return data;
    } else {
      const text = await response.text();
      console.log(`[API] Response text:`, text);
      return text as T;
    }
  } catch (error) {
    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        console.error('[API] Timeout error:', endpoint);
        throw new Error(`Request timeout for ${endpoint}`);
      }
      console.error('[API] Fetch error:', error.message);
      throw error;
    }
    throw new Error('Unknown API error');
  }
}

/**
 * POST request helper
 */
export async function apiPost<T>(
  endpoint: string,
  data: unknown
): Promise<T> {
  console.log(`[API] Sending POST data:`, data);
  return apiFetch<T>(endpoint, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
    body: JSON.stringify(data),
  });
}

/**
 * GET request helper
 */
export async function apiGet<T>(endpoint: string): Promise<T> {
  return apiFetch<T>(endpoint, {
    method: 'GET',
    headers: {
      'Accept': 'application/json',
    },
  });
}
