import { useState } from 'react';
import {
  updateGlobalState,
  deleteFlag,
  setUserOverride,
  removeUserOverride,
  setGroupOverride,
  removeGroupOverride,
} from '../api/flags';

export default function FlagCard({ flag, onRefresh }) {
  const [newUserId, setNewUserId] = useState('');
  const [newUserEnabled, setNewUserEnabled] = useState(true);
  const [newGroupId, setNewGroupId] = useState('');
  const [newGroupEnabled, setNewGroupEnabled] = useState(true);
  const [error, setError] = useState('');

  const handleToggleGlobal = async () => {
    setError('');
    try {
      await updateGlobalState(flag.name, !flag.isEnabled);
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleDelete = async () => {
    if (!confirm(`Delete flag "${flag.name}"?`)) return;
    try {
      await deleteFlag(flag.name);
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleAddUserOverride = async (e) => {
    e.preventDefault();
    if (!newUserId.trim()) return;
    try {
      await setUserOverride(flag.name, newUserId.trim(), newUserEnabled);
      setNewUserId('');
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleRemoveUser = async (userId) => {
    try {
      await removeUserOverride(flag.name, userId);
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleAddGroupOverride = async (e) => {
    e.preventDefault();
    if (!newGroupId.trim()) return;
    try {
      await setGroupOverride(flag.name, newGroupId.trim(), newGroupEnabled);
      setNewGroupId('');
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleRemoveGroup = async (groupId) => {
    try {
      await removeGroupOverride(flag.name, groupId);
      onRefresh();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="flag-card">
      <div className="flag-header">
        <div>
          <h3>{flag.name}</h3>
          {flag.description && <p className="desc">{flag.description}</p>}
        </div>
        <div className="flag-actions">
          <button
            className={`toggle-btn ${flag.isEnabled ? 'on' : 'off'}`}
            onClick={handleToggleGlobal}
          >
            {flag.isEnabled ? '🟢 ON' : '🔴 OFF'}
          </button>
          <button className="delete-btn" onClick={handleDelete}>🗑️</button>
        </div>
      </div>

      {/* User Overrides */}
      <div className="overrides-section">
        <h4>👤 User Overrides</h4>
        {flag.userOverrides.length === 0 && <p className="empty">None</p>}
        {flag.userOverrides.map((uo) => (
          <div key={uo.userId} className="override-item">
            <span>{uo.userId}: {uo.isEnabled ? '✅ ON' : '❌ OFF'}</span>
            <button className="remove-btn" onClick={() => handleRemoveUser(uo.userId)}>✕</button>
          </div>
        ))}
        <form onSubmit={handleAddUserOverride} className="add-override-form">
          <input
            type="text"
            placeholder="User ID"
            value={newUserId}
            onChange={(e) => setNewUserId(e.target.value)}
          />
          <label>
            <input
              type="checkbox"
              checked={newUserEnabled}
              onChange={(e) => setNewUserEnabled(e.target.checked)}
            />
            On
          </label>
          <button type="submit">Add</button>
        </form>
      </div>

      {/* Group Overrides */}
      <div className="overrides-section">
        <h4>👥 Group Overrides</h4>
        {flag.groupOverrides.length === 0 && <p className="empty">None</p>}
        {flag.groupOverrides.map((go) => (
          <div key={go.groupId} className="override-item">
            <span>{go.groupId}: {go.isEnabled ? '✅ ON' : '❌ OFF'}</span>
            <button className="remove-btn" onClick={() => handleRemoveGroup(go.groupId)}>✕</button>
          </div>
        ))}
        <form onSubmit={handleAddGroupOverride} className="add-override-form">
          <input
            type="text"
            placeholder="Group ID"
            value={newGroupId}
            onChange={(e) => setNewGroupId(e.target.value)}
          />
          <label>
            <input
              type="checkbox"
              checked={newGroupEnabled}
              onChange={(e) => setNewGroupEnabled(e.target.checked)}
            />
            On
          </label>
          <button type="submit">Add</button>
        </form>
      </div>

      {error && <p className="error">{error}</p>}
    </div>
  );
}
