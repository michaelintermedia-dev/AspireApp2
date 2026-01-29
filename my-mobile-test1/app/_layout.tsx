import { Slot } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { StyleSheet, View } from 'react-native';
import { useEffect } from 'react';

import { setupNotificationHandler, registerForPushNotifications, registerDeviceWithBackend } from '../api/notifications';

async function initializePushNotifications() {
  try {
    console.log('[App] Initializing push notifications...');
    
    // Step 1: Get FCM token
    const token = await registerForPushNotifications();
    
    if (!token) {
      console.log('[App] Could not obtain push token (device might not support it)');
      return;
    }

    console.log('[App] Got token, attempting backend register...');

    // Step 2: Register device with backend
    const registered = await registerDeviceWithBackend(token);
    
    if (registered) {
      console.log('[App] ✅ Push notifications initialized successfully');
    } else {
      console.log('[App] ⚠️ Push notifications registered locally but backend registration failed');
    }
  } catch (error) {
    console.error('[App] Failed to initialize push notifications:', error);
  }
}

export default function RootLayout() {
  useEffect(() => {
    setupNotificationHandler();
    initializePushNotifications();
  }, []);

  return (
    <View style={styles.container}>
      <Slot />
      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
});
