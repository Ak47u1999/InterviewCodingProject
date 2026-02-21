import { useState, useEffect, useCallback } from 'react';
import { getFlags } from './api/flags';
import CreateFlagForm from './components/CreateFlagForm';
import FlagCard from './components/FlagCard';
import EvaluatePanel from './components/EvaluatePanel';
import './App.css';

function App() {
  const [flags, setFlags] = useState([]);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getFlags();
      setFlags(data);
    } catch {
      console.error('Failed to load flags');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return (
    <div className="app">
      <header>
        <h1>🚩 Feature Flag Engine</h1>
        <p>Create, toggle, and evaluate feature flags in real time</p>
      </header>

      <CreateFlagForm onCreated={refresh} />
      <EvaluatePanel />

      <section className="flags-list">
        <h2>All Flags {!loading && <span className="count">({flags.length})</span>}</h2>
        {loading && <p>Loading...</p>}
        {!loading && flags.length === 0 && <p className="empty">No flags yet. Create one above!</p>}
        {flags.map((f) => (
          <FlagCard key={f.name} flag={f} onRefresh={refresh} />
        ))}
      </section>
    </div>
  );
}

export default App;
