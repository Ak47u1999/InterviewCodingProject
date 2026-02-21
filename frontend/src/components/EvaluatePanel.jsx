import { useState } from 'react';
import { evaluateFlag } from '../api/flags';

export default function EvaluatePanel() {
  const [flagName, setFlagName] = useState('');
  const [userId, setUserId] = useState('');
  const [groupIds, setGroupIds] = useState('');
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');

  const handleEvaluate = async (e) => {
    e.preventDefault();
    setError('');
    setResult(null);
    try {
      const groups = groupIds.trim() ? groupIds.split(',').map((g) => g.trim()) : [];
      const res = await evaluateFlag(flagName.trim(), userId.trim(), groups);
      setResult(res);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div className="evaluate-panel">
      <h3>🔍 Evaluate Flag</h3>
      <form onSubmit={handleEvaluate} className="form-row">
        <input
          type="text"
          placeholder="Flag name"
          value={flagName}
          onChange={(e) => setFlagName(e.target.value)}
          required
        />
        <input
          type="text"
          placeholder="User ID (optional)"
          value={userId}
          onChange={(e) => setUserId(e.target.value)}
        />
        <input
          type="text"
          placeholder="Groups (comma-separated)"
          value={groupIds}
          onChange={(e) => setGroupIds(e.target.value)}
        />
        <button type="submit">Evaluate</button>
      </form>
      {result && (
        <div className={`eval-result ${result.isEnabled ? 'enabled' : 'disabled'}`}>
          <strong>{result.flagName}</strong>: {result.isEnabled ? '✅ ENABLED' : '❌ DISABLED'}
        </div>
      )}
      {error && <p className="error">{error}</p>}
    </div>
  );
}
