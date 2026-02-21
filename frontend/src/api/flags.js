const API_BASE = '/api';

export async function getFlags() {
  const res = await fetch(`${API_BASE}/flags`);
  return res.json();
}

export async function getFlag(name) {
  const res = await fetch(`${API_BASE}/flags/${name}`);
  if (!res.ok) throw new Error(`Flag '${name}' not found`);
  return res.json();
}

export async function createFlag(name, isEnabled, description) {
  const res = await fetch(`${API_BASE}/flags`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, isEnabled, description }),
  });
  if (!res.ok) {
    const err = await res.json();
    throw new Error(err.error);
  }
  return res.json();
}

export async function updateGlobalState(name, isEnabled) {
  const res = await fetch(`${API_BASE}/flags/${name}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isEnabled }),
  });
  if (!res.ok) throw new Error('Failed to update flag');
}

export async function deleteFlag(name) {
  const res = await fetch(`${API_BASE}/flags/${name}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Failed to delete flag');
}

export async function evaluateFlag(name, userId, groupIds) {
  const res = await fetch(`${API_BASE}/flags/${name}/evaluate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId: userId || null, groupIds: groupIds?.length ? groupIds : null }),
  });
  if (!res.ok) throw new Error('Evaluation failed');
  return res.json();
}

export async function setUserOverride(flagName, userId, isEnabled) {
  const res = await fetch(`${API_BASE}/flags/${flagName}/users/${userId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isEnabled }),
  });
  if (!res.ok) throw new Error('Failed to set user override');
}

export async function removeUserOverride(flagName, userId) {
  const res = await fetch(`${API_BASE}/flags/${flagName}/users/${userId}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Failed to remove user override');
}

export async function setGroupOverride(flagName, groupId, isEnabled) {
  const res = await fetch(`${API_BASE}/flags/${flagName}/groups/${groupId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isEnabled }),
  });
  if (!res.ok) throw new Error('Failed to set group override');
}

export async function removeGroupOverride(flagName, groupId) {
  const res = await fetch(`${API_BASE}/flags/${flagName}/groups/${groupId}`, { method: 'DELETE' });
  if (!res.ok) throw new Error('Failed to remove group override');
}
