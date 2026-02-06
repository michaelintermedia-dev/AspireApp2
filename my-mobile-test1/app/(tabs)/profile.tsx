import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, Alert, Platform } from 'react-native';
import { useAuth } from '../../auth/hooks/useAuth';
import { ProtectedRoute } from '../../components/ProtectedRoute';

function ProfileScreenContent() {
  const { user, logout, isAuthenticated } = useAuth();

  const handleLogout = async () => {
    // Platform-specific confirmation
    if (Platform.OS === 'web') {
      // Use browser's native confirm dialog
      if (window.confirm('Are you sure you want to logout?')) {
        try {
          await logout();
        } catch (error) {
          console.error('Logout error:', error);
          alert('Logout failed. Please try again.');
        }
      }
    } else {
      // Use React Native Alert for mobile
      Alert.alert(
        'Logout',
        'Are you sure you want to logout?',
        [
          { text: 'Cancel', style: 'cancel' },
          {
            text: 'Logout',
            style: 'destructive',
            onPress: async () => {
              try {
                await logout();
              } catch (error) {
                console.error('Logout error:', error);
                Alert.alert('Error', 'Logout failed. Please try again.');
              }
            },
          },
        ]
      );
    }
  };

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Profile</Text>
      </View>

      <View style={styles.content}>
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Account Information</Text>
          
          <View style={styles.infoRow}>
            <Text style={styles.label}>User ID:</Text>
            <Text style={styles.value}>{user.id}</Text>
          </View>

          <View style={styles.infoRow}>
            <Text style={styles.label}>Email:</Text>
            <Text style={styles.value}>{user.email}</Text>
          </View>
        </View>

        <TouchableOpacity style={styles.logoutButton} onPress={handleLogout}>
          <Text style={styles.logoutButtonText}>Logout</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

export default function ProfileScreen() {
  return (
    <ProtectedRoute>
      <ProfileScreenContent />
    </ProtectedRoute>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  header: {
    backgroundColor: '#fff',
    paddingTop: 60,
    paddingBottom: 20,
    paddingHorizontal: 24,
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  title: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#000',
  },
  content: {
    flex: 1,
    padding: 24,
  },
  section: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 20,
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000',
    marginBottom: 16,
  },
  infoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  label: {
    fontSize: 16,
    color: '#666',
  },
  value: {
    fontSize: 16,
    color: '#000',
    fontWeight: '500',
  },
  logoutButton: {
    backgroundColor: '#ff3b30',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    marginTop: 'auto',
  },
  logoutButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});
