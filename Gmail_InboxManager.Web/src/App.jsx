import { useEffect, useState } from "react";

const API_BASE_URL = "https://localhost:7202";

export default function App() {
  const [authorized, setAuthorized] = useState(false);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState("");

  async function loadLatestMessage() {
    setError("");

    try {
      const response = await fetch(`${API_BASE_URL}/api/test-gmail`);

      if (response.status === 401) {
        setAuthorized(false);
        setMessage(null);
        return;
      }

      if (!response.ok) {
        throw new Error(`Nie udało się pobrać wiadomości (${response.status}).`);
      }

      const contentType = response.headers.get("content-type") ?? "";
      if (!contentType.includes("application/json")) {
        setAuthorized(false);
        setMessage(null);
        return;
      }

      setAuthorized(true);
      setMessage(await response.json());
    } catch (requestError) {
      setError(requestError.message);
    }
  }

  async function checkStatus() {
    setLoading(true);
    setError("");

    try {
      await loadLatestMessage();
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    checkStatus();
  }, []);

  function connectGmail() {
    window.location.assign(`${API_BASE_URL}/login`);
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="/" aria-label="Gmail Inbox Manager">
          <span className="brand-mark" aria-hidden="true">
            M
          </span>
          <span>
            <strong>Inbox Manager</strong>
            <small>Inteligentne filtrowanie</small>
          </span>
        </a>

        <nav className="navigation" aria-label="Nawigacja główna">
          <a className="nav-item nav-item-active" href="/">
            <span aria-hidden="true">⌂</span>
            Przegląd
          </a>
          <a className="nav-item" href="#messages">
            <span aria-hidden="true">✉</span>
            Wiadomości
          </a>
          <a className="nav-item" href="#filters">
            <span aria-hidden="true">≡</span>
            Filtry i reguły
          </a>
          <a className="nav-item" href="#security">
            <span aria-hidden="true">◆</span>
            Prywatność
          </a>
        </nav>

        <div className={`connection connection-${authorized ? "online" : "offline"}`}>
          <span className="status-dot" />
          <span>
            <strong>{authorized ? "Gmail połączony" : "Gmail rozłączony"}</strong>
            <small>
              {loading
                ? "Sprawdzanie..."
                : authorized
                  ? "Synchronizacja aktywna"
                  : "Wymagana autoryzacja"}
            </small>
          </span>
        </div>
      </aside>

      <main className="main-content">
        <header className="topbar">
          <div>
            <p className="eyebrow">Panel zarządzania</p>
            <h1>Przegląd skrzynki</h1>
          </div>
          <div className="topbar-actions">
            {authorized ? (
              <button className="button button-secondary" type="button" onClick={loadLatestMessage}>
                Odśwież
              </button>
            ) : (
              <button className="button button-primary" type="button" onClick={connectGmail}>
                Połącz z Gmail
              </button>
            )}
          </div>
        </header>

        {error && <p className="error">{error}</p>}

        <section className="summary-grid" aria-label="Podsumowanie skrzynki">
          <article className="summary-card">
            <span className="summary-icon summary-icon-blue" aria-hidden="true">✉</span>
            <div>
              <p>Najnowsza wiadomość</p>
              <strong>{message?.id ? "Dostępna" : "Brak danych"}</strong>
            </div>
          </article>
          <article className="summary-card">
            <span className="summary-icon summary-icon-red" aria-hidden="true">⚠</span>
            <div>
              <p>Potencjalny spam</p>
              <strong>Do analizy</strong>
            </div>
          </article>
          <article className="summary-card">
            <span className="summary-icon summary-icon-green" aria-hidden="true">✓</span>
            <div>
              <p>Status połączenia</p>
              <strong>{authorized ? "Aktywne" : "Niepołączone"}</strong>
            </div>
          </article>
        </section>

        <section className="content-grid">
          <article className="panel-card message-panel" id="messages">
            <header className="card-header">
              <div>
                <p className="eyebrow">Szybki podgląd</p>
                <h2>Najnowsza wiadomość</h2>
              </div>
              {authorized && (
                <span className="badge badge-success">Połączono</span>
              )}
            </header>

            {!authorized && !loading ? (
              <div className="empty-state">
                <span className="empty-icon" aria-hidden="true">✉</span>
                <h3>Połącz aplikację z Gmail</h3>
                <p>Zaloguj się przez Google, aby pobrać najnowszą wiadomość ze swojej skrzynki.</p>
                <button className="button button-primary" type="button" onClick={connectGmail}>
                  Rozpocznij logowanie
                </button>
              </div>
            ) : loading ? (
              <div className="empty-state">
                <span className="empty-icon" aria-hidden="true">…</span>
                <h3>Sprawdzanie połączenia</h3>
              </div>
            ) : message?.id ? (
              <div className="message-preview">
                <p className="message-id">ID: {message.id}</p>
                <h3>{message.temat || "(Brak tematu)"}</h3>
                <div className="message-body">{message.tresc || "(Brak treści)"}</div>
              </div>
            ) : (
              <div className="empty-state">
                <span className="empty-icon" aria-hidden="true">✓</span>
                <h3>Brak wiadomości do wyświetlenia</h3>
                <p>Połączona skrzynka nie zawiera wiadomości lub nie udało się ich pobrać.</p>
              </div>
            )}
          </article>

          <aside className="side-panels">
            <section className="panel-card" id="filters">
              <header className="card-header">
                <div>
                  <p className="eyebrow">Automatyzacja</p>
                  <h2>Filtry i reguły</h2>
                </div>
              </header>
              <div className="rule-list">
                <div className="rule-row">
                  <span className="rule-icon" aria-hidden="true">◎</span>
                  <div>
                    <strong>Kody 2FA</strong>
                    <small>Wykrywanie i propozycja usunięcia</small>
                  </div>
                  <span className="badge">Wkrótce</span>
                </div>
                <div className="rule-row">
                  <span className="rule-icon" aria-hidden="true">⌕</span>
                  <div>
                    <strong>Niechciani nadawcy</strong>
                    <small>Analiza zaangażowania</small>
                  </div>
                  <span className="badge">Wkrótce</span>
                </div>
                <div className="rule-row">
                  <span className="rule-icon" aria-hidden="true">!</span>
                  <div>
                    <strong>Kwarantanna spamu</strong>
                    <small>Ocena przed wykonaniem działania</small>
                  </div>
                  <span className="badge">Wkrótce</span>
                </div>
              </div>
            </section>

            <section className="panel-card privacy-card" id="security">
              <p className="eyebrow">Bezpieczeństwo</p>
              <h2>Kontrola należy do Ciebie</h2>
              <p>
                Każda propozycja filtrowania, blokady lub usunięcia powinna zostać
                potwierdzona przez użytkownika.
              </p>
            </section>
          </aside>
        </section>
      </main>
    </div>
  );
}
