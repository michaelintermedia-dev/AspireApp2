import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import { Platform } from 'react-native';
import { apiPost } from './client';
import { RegisterDeviceRequest, RegisterDeviceResponse } from './types';

/**
 * Register device for push notifications using FCM
 */
export async function registerForPushNotifications(): Promise<string | null> {
  try {
    // Only works on physical devices
    if (!Device.isDevice) {
      console.log('[Push] Notifications work only on physical devices');
      return null;
    }

    // Request permissions
    const { status: existingStatus } = await Notifications.getPermissionsAsync();
    let finalStatus = existingStatus;

    if (existingStatus !== 'granted') {
      console.log('[Push] Requesting push permissions...');
      const { status } = await Notifications.requestPermissionsAsync();
      finalStatus = status;
    }

    if (finalStatus !== 'granted') {
      console.log('[Push] Push permission denied');
      return null;
    }

    // Get FCM token
    try {
      const tokenResponse = await Notifications.getDevicePushTokenAsync();
      const token = tokenResponse.data;
      console.log('[Push] FCM Token obtained:', token.substring(0, 20) + '...');
      return token;
    } catch (error) {
      console.error('[Push] Failed to get FCM token:', error);
      return null;
    }
  } catch (error) {
    console.error('[Push] Failed to register for push notifications:', error);
    return null;
  }
}

/**
 * Register device token with backend
 */
export async function registerDeviceWithBackend(token: string): Promise<boolean> {
  try {
    const request: RegisterDeviceRequest = {
      token,
      platform: Platform.OS === 'ios' ? 'iOS' : Platform.OS === 'android' ? 'Android' : 'Web',
    };

    console.log('[Push] Registering device with backend...', request.platform);

    const response = await apiPost<RegisterDeviceResponse>(
      '/devices/register',
      request
    );

    console.log('[Push] Device registered successfully');
    return response.success ?? true;
  } catch (error) {
    console.error('[Push] Failed to register device with backend:', error);
    return false;
  }
}

/**
 * Setup notification handler
 */
export function setupNotificationHandler() {
  Notifications.setNotificationHandler({
    handleNotification: async () => ({
      shouldShowAlert: true,
      shouldShowBanner: true,
      shouldShowList: true,
      shouldPlaySound: true,
      shouldSetBadge: false,
    }),
  });

  // Handle notification when app is in foreground
  const subscription = Notifications.addNotificationReceivedListener((notification) => {
    console.log('[Push] Notification received (foreground):', notification.request.content.body);
  });

  // Handle notification when user taps on it
  const responseSubscription = Notifications.addNotificationResponseReceivedListener((response) => {
    console.log('[Push] Notification tapped:', response.notification.request.content.body);
  });

  return () => {
    subscription.remove();
    responseSubscription.remove();
  };
}
