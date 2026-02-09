import { RecordingPresets, requestRecordingPermissionsAsync, useAudioPlayer, useAudioRecorder } from 'expo-audio';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
//import '../../firebase';
import { apiGet, getApiUrl } from '../../api/client';
import { TokenService } from '../../auth/services/TokenService';

function RecordingItem({ uri, index }: { uri: string; index: number }) {
    const player = useAudioPlayer(uri);

    async function uploadRecording() {
        try {
            console.log('[Upload] Starting upload:', uri);

            // Generate unique filename based on timestamp
            const now = new Date();
            const timestamp = now.toISOString().replace(/[-:]/g, '').replace('T', '_').split('.')[0];
            const filename = `recording_${timestamp}.m4a`;

            const formData = new FormData();
            if (Platform.OS === 'web') {
                const audioResponse = await fetch(uri);
                const blob = await audioResponse.blob();
                formData.append('file', blob, filename);
            } else {
                // @ts-ignore
                formData.append('file', {
                    uri: uri,
                    name: filename,
                    type: 'audio/m4a',
                });
            }

            const apiUrl = getApiUrl('/UploadAudio');
            const token = await TokenService.getToken();
            console.log('[Upload] Uploading to:', apiUrl);

            // Create abort controller for timeout
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout

            try {
                const response = await fetch(apiUrl, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        ...(token && { 'Authorization': `Bearer ${token}` }),
                    },
                    body: formData,
                    signal: controller.signal,
                });

                clearTimeout(timeoutId);

                if (response.ok) {
                    const result = await response.text();
                    console.log('[Upload] Success:', result);
                    alert('Upload successful!');
                } else {
                    const errorText = await response.text();
                    console.error('[Upload] Failed', response.status, errorText);
                    alert(`Upload failed: ${response.status} - ${errorText}`);
                }
            } catch (fetchErr: any) {
                clearTimeout(timeoutId);
                if (fetchErr.name === 'AbortError') {
                    console.error('[Upload] Timeout');
                    alert('Upload timeout - cannot reach server at ' + apiUrl);
                } else {
                    throw fetchErr;
                }
            }
        } catch (err: any) {
            console.error('[Upload] Error', err);
            alert(`Upload error: ${err.message || err}`);
        }
    }

    return (
        <View style={styles.recordingItemContainer}>
            <Pressable onPress={() => {
                player.seekTo(0);
                player.play();
            }} style={styles.playbackArea}>
                <View style={styles.recordingItem}>
                    <Text>Recording {index + 1}: {uri.split('/').pop()}</Text>
                </View>
            </Pressable>
            <Pressable onPress={uploadRecording} style={styles.saveButton}>
                <Text style={styles.saveButtonText}>Save</Text>
            </Pressable>
        </View>
    );
}

export default function Index() {
const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
const [recordings, setRecordings] = useState<string[]>([]);
const [isRecording, setIsRecording] = useState(false);
const [cmsContent, setCmsContent] = useState<Record<string, any> | null>(null);
const [cmsLoading, setCmsLoading] = useState(true);

useEffect(() => {
    async function fetchCmsContent() {
        try {
            setCmsLoading(true);
            const data = await apiGet<Record<string, any>>('/cms/content/contenttest1');
            console.log('[CMS] Fetched content:', data);
            setCmsContent(data);
        } catch (err: any) {
            console.error('[CMS] Fetch error:', err);
        } finally {
            setCmsLoading(false);
        }
    }
    fetchCmsContent();
}, []);

    async function startRecording() {
        try {
            const permission = await requestRecordingPermissionsAsync();
            if (permission.status !== 'granted') {
                console.log("Permission not granted");
                return;
            }

            await recorder.prepareToRecordAsync();
            recorder.record();
            setIsRecording(true);
            console.log('Recording started');
        } catch (err) {
            console.error('Failed to start recording', err);
        }
    }

    async function stopRecording() {
        if (!isRecording) return;

        console.log('Stopping recording..');
        await recorder.stop();
        setIsRecording(false);

        // We get the URI from the recorder instance
        const uri = recorder.uri;
        console.log('Recording stopped and stored at', uri);
        if (uri) {
            setRecordings(prev => [...prev, uri]);
        }
    }

    async function checkServerLiveness() {
        try {
            const apiUrl = getApiUrl('/healthz/live');
            console.log('[Server] Checking liveness:', apiUrl);

            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout

            const response = await fetch(apiUrl, {
                method: 'GET',
                signal: controller.signal,
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                const result = await response.text();
                console.log('[Server] Alive:', result);
                alert(`? Server is alive!\n${result}`);
            } else {
                console.error('[Server] Check failed:', response.status);
                alert(`? Server returned: ${response.status}`);
            }
        } catch (err: any) {
            console.error('[Server] Error:', err);
            if (err.name === 'AbortError') {
                alert('? Server timeout - cannot reach server');
            } else {
                alert(`? Server error: ${err.message || err}`);
            }
        }
    }

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Audio Recorder</Text>

            {cmsLoading ? (
                <ActivityIndicator size="small" color="#007AFF" style={styles.cmsLoading} />
            ) : cmsContent ? (
                <View style={styles.cmsContainer}>
                    {Object.entries(cmsContent).map(([key, value]) => (
                        <Text key={key} style={styles.cmsText}>
                            {typeof value === 'string' ? value : JSON.stringify(value)}
                        </Text>
                    ))}
                </View>
            ) : null}

            <View style={styles.buttonContainer}>
                <Pressable
                    onPressIn={startRecording}
                    onPressOut={stopRecording}
                    style={({ pressed }) => [
                        styles.recordButton,
                        pressed ? styles.recordButtonPressed : null
                    ]}
                >
                    <Text style={styles.recordButtonText}>
                        {isRecording ? 'Recording...' : 'Hold to Record'}
                    </Text>
                </Pressable>
            </View>

            <FlatList
                data={recordings}
                keyExtractor={(item, index) => index.toString()}
                renderItem={({ item, index }) => (
                    <RecordingItem uri={item} index={index} />
                )}
                style={styles.list}
            />

            <Pressable onPress={checkServerLiveness} style={styles.livenessButton}>
                <Text style={styles.livenessButtonText}>Server Liveness Probe</Text>
            </Pressable>
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        alignItems: 'center',
        justifyContent: 'center',
        paddingTop: 50,
        backgroundColor: '#fff',
    },
    title: {
        fontSize: 24,
        fontWeight: 'bold',
        marginBottom: 20,
    },
    buttonContainer: {
        marginBottom: 20,
    },
    recordButton: {
        backgroundColor: '#007AFF',
        paddingVertical: 15,
        paddingHorizontal: 30,
        borderRadius: 25,
        minWidth: 200,
        alignItems: 'center',
    },
    recordButtonPressed: {
        backgroundColor: '#FF3B30',
    },
    recordButtonText: {
        color: '#fff',
        fontSize: 18,
        fontWeight: '600',
    },
    list: {
        flex: 1,
        width: '100%',
        paddingHorizontal: 20,
    },
    recordingItem: {
        padding: 15,
    },
    recordingItemContainer: {
        flexDirection: 'row',
        alignItems: 'center',
        borderBottomWidth: 1,
        borderBottomColor: '#eee',
        paddingRight: 15,
    },
    playbackArea: {
        flex: 1,
    },
    saveButton: {
        backgroundColor: '#34C759',
        paddingVertical: 8,
        paddingHorizontal: 15,
        borderRadius: 15,
    },
    saveButtonText: {
        color: '#fff',
        fontWeight: 'bold',
    },
    livenessButton: {
        backgroundColor: '#5856D6',
        paddingVertical: 12,
        paddingHorizontal: 20,
        borderRadius: 20,
        marginBottom: 20,
        marginTop: 10,
    },
    livenessButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: '600',
    },
    cmsLoading: {
        marginBottom: 15,
    },
    cmsContainer: {
        backgroundColor: '#F0F4FF',
        borderRadius: 12,
        padding: 15,
        marginHorizontal: 20,
        marginBottom: 15,
        width: '90%',
    },
    cmsText: {
        fontSize: 14,
        color: '#333',
        marginBottom: 4,
    },
});
