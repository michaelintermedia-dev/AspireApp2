import { StyleSheet, Text, View } from 'react-native';

interface DebugPanelProps {
    message: string;
    type?: 'info' | 'error' | 'success' | 'warning';
}

export function DebugPanel({ message, type = 'info' }: DebugPanelProps) {
    const colors = {
        info: '#007AFF',
        error: '#FF3B30',
        success: '#34C759',
        warning: '#FF9500',
    };

    return (
        <View style={[styles.container, { borderLeftColor: colors[type] }]}>
            <Text style={[styles.text, { color: colors[type] }]}>
                {type === 'success' ? '?' : type === 'error' ? '?' : type === 'warning' ? '??' : '??'} {message}
            </Text>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        backgroundColor: '#f8f8f8',
        borderLeftWidth: 4,
        paddingHorizontal: 12,
        paddingVertical: 8,
        marginHorizontal: 16,
        marginVertical: 4,
        borderRadius: 4,
    },
    text: {
        fontSize: 12,
        fontFamily: 'Courier New',
    },
});
