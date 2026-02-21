import { useState } from 'react';
import { createFlag } from '../api/flags';

export default function CreateFlagForm({ onCreated }) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isEnabled, setIsEnabled] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    try {
      await createFlag(name.trim(), isEnabled, description.trim() || null);
      setName('');
      setDescription('');
      setIsEnabled(false);
      onCreated();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="create-form">
      <h3>➕ Create Feature Flag</h3>
      <div className="form-row">
        <input
          type="text"
          placeholder="Flag name (e.g. dark-mode)"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <input
          type="text"
          placeholder="Description (optional)"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <label className="toggle-label">
          <input
            type="checkbox"
            checked={isEnabled}
            onChange={(e) => setIsEnabled(e.target.checked)}
          />
          Enabled
        </label>
        <button type="submit">Create</button>
      </div>
      {error && <p className="error">{error}</p>}
    </form>
  );
}
