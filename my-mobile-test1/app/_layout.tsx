import { Slot } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { StyleSheet, View } from 'react-native';
import { useEffect } from 'react';

import { setupNotificationHandler, registerForPushNotifications } from '../api/notifications';
import { AuthProvider } from '../auth/context/AuthContext';

export default function RootLayout() {
  useEffect(() => {
    setupNotificationHandler();
    // Request push permissions early; device registration happens after auth in AuthContext
    registerForPushNotifications();
  }, []);

  return (
    <AuthProvider>
      <View style={styles.container}>
        <Slot />
        <StatusBar style="auto" />
      </View>
    </AuthProvider>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
});
