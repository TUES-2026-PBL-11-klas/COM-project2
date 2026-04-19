import React, { useState } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import { API_URL } from '../../src/constants/api';
import { getToken, getUserId } from '../../src/utils/storage';
import eventBus from '../../src/utils/eventBus';

const SUBJECTS = [
  'Mathematics', 'Physics', 'Chemistry', 'Biology', 'Computer Science', 'English', 'History'
];

export default function CreateMentor() {
  const router = useRouter();
  const [selected, setSelected] = useState<string[]>([]);
  const [creating, setCreating] = useState(false);

  const toggle = (s: string) => {
    setSelected(prev => prev.includes(s) ? prev.filter(x => x !== s) : [...prev, s]);
  };

  const handleCreate = async () => {
    if (selected.length === 0) return;
    setCreating(true);
    try {
      const token = await getToken();
      const userId = await getUserId();
      // POST to create mentor profile
      // Prefer authenticated token (server will resolve user from token). Only send userId when no token is available.
      const body: any = { subjects: selected };
      if (!token && userId) body.userId = userId;
      await fetch(`${API_URL}/mentors`, {
        method: 'POST', headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: JSON.stringify(body)
      });

      // grant mentor role (server should handle role assignment in mentor creation endpoint)

      // server creates an initial public chat for the mentor; no client-side chat POST needed

      // notify other views: emit mentorCreated with username if available
      try {
        let username: string | null = null;
        let createdUserId = userId || null;
        try {
          const token2 = await getToken();
          if (token2) {
            const r = await fetch(`${API_URL}/auth/me`, { headers: { Authorization: `Bearer ${token2}` } });
            if (r.ok) {
              const j = await r.json();
              username = j.username || null;
              createdUserId = j.id || createdUserId;
            }
          }
        } catch { }
        eventBus.emit('mentorCreated', { userId: createdUserId, username });
        eventBus.emit('reviewUpdated', { reviewedUserId: userId });
      } catch {}

      // navigate back to main page (do not show self in mentor selector)
      router.replace('/');
    } catch (err) {
      console.warn('Create mentor failed', err);
    } finally {
      setCreating(false);
    }
  };

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>Create Mentor Profile</Text>
      <Text style={styles.subtitle}>Select one or more subjects you can teach</Text>
      <View style={{ marginTop: 18 }}>
        {SUBJECTS.map(s => (
          <TouchableOpacity key={s} onPress={() => toggle(s)} style={[styles.subjectRow, selected.includes(s) && styles.subjectRowActive]}>
            <Text style={[styles.subjectText, selected.includes(s) && { fontWeight: '700' }]}>{s}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <TouchableOpacity style={[styles.createButton, selected.length === 0 && styles.disabled]} onPress={handleCreate} disabled={selected.length === 0 || creating}>
        <Text style={styles.createButtonText}>{creating ? 'Creating…' : 'Create Mentor Profile'}</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 20, paddingTop: 48, backgroundColor: '#EEF2FF', minHeight: '100%' },
  title: { fontSize: 24, fontWeight: '700', color: '#0F172A' },
  subtitle: { marginTop: 8, color: '#64748B' },
  subjectRow: { padding: 12, backgroundColor: '#fff', borderRadius: 12, marginBottom: 10 },
  subjectRowActive: { backgroundColor: '#E0F2FE' },
  subjectText: { color: '#0F172A' },
  createButton: { marginTop: 20, backgroundColor: '#2563EB', paddingVertical: 14, borderRadius: 12, alignItems: 'center' },
  createButtonText: { color: '#fff', fontWeight: '700' },
  disabled: { backgroundColor: '#94A3B8' }
});
