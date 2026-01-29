/**
 * API Request/Response Types
 */

export interface RegisterDeviceRequest {
  token: string;
  platform: 'Android' | 'iOS' | 'Web';
}

export interface RegisterDeviceResponse {
  success: boolean;
  message?: string;
}

export interface ServerHealthResponse {
  status: string;
  timestamp?: string;
}
