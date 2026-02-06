import { MaterialIcons } from '@expo/vector-icons';
import { Tabs } from 'expo-router';
import { ProtectedRoute } from '../../components/ProtectedRoute';

export default function TabLayout() {
    return (
        <ProtectedRoute>
            <Tabs
                screenOptions={{
                    tabBarActiveTintColor: '#007AFF',
                    headerShown: false,
                }}
            >
                <Tabs.Screen
                    name="index"
                    options={{
                        title: 'Recorder',
                        tabBarIcon: ({ color, size }) => (
                            <MaterialIcons name="mic" size={size} color={color} />
                        ),
                    }}
                />
                <Tabs.Screen
                    name="recordings"
                    options={{
                        title: 'Recordings',
                        tabBarIcon: ({ color, size }) => (
                            <MaterialIcons name="library-music" size={size} color={color} />
                        ),
                    }}
                />
            </Tabs>
        </ProtectedRoute>
    );
}