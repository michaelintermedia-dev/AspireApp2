import { Platform } from 'react-native';
import * as Device from 'expo-device';
import { AuthenticatedApiClient } from '../auth/services/AuthenticatedApiClient';

/**
 * Register device for push notifications with authentication
 */
export async function registerDeviceWithBackend(fcmToken: string): Promise<boolean> {
  try {
    console.log('[API] Registering device with backend (authenticated)...');
    
    await AuthenticatedApiClient.post('/devices/register', {
      token: fcmToken,
      platform: Platform.OS,
      deviceModel: Device.modelName || 'Unknown',
      appVersion: '1.0.0', // You might want to get this from app.json
    });

    console.log('[API] Device registered successfully');
    return true;
  } catch (error) {
    console.error('[API] Failed to register device:', error);
    
    // If authentication error, you might want to handle it differently
    if (error instanceof Error && error.message.includes('Not authenticated')) {
      console.log('[API] User not authenticated, skipping device registration');
    }
    
    return false;
  }
}

/**
 * Example: Get user's devices
 */
export async function getUserDevices() {
  try {
    const devices = await AuthenticatedApiClient.get('/devices/list');
    return devices;
  } catch (error) {
    console.error('[API] Failed to get user devices:', error);
    throw error;
  }
}

/**
 * Example: Delete a device
 */
export async function deleteDevice(deviceId: string) {
  try {
    await AuthenticatedApiClient.delete(`/devices/${deviceId}`);
    console.log('[API] Device deleted successfully');
  } catch (error) {
    console.error('[API] Failed to delete device:', error);
    throw error;
  }
}
