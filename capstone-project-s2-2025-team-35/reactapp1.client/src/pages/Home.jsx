import EventGrid from "../components/EventGrid";
import EventCard from "../components/EventCard";
// Removed localStorage dependency - user info now comes from /auth/me API
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

const API_BASE_URL = 'http://localhost:5089/api';

export default function Home() {
    const [events, setEvents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [q, setQ] = useState("");
    const [cat, setCat] = useState("");
    const navigate = useNavigate();
    // User info will be retrieved from Header component or /auth/me API
    const [user, setUser] = useState(null);

    // Fetch user info from /auth/me API
    useEffect(() => {
        const fetchUser = async () => {
            try {
                const response = await fetch(`${API_BASE_URL}/auth/me`, {
                    credentials: 'include'
                });
                if (response.ok) {
                    const userData = await response.json();
                    setUser(userData);
                }
            } catch (error) {
                console.error('Error fetching user:', error);
            }
        };
        fetchUser();
    }, []);

    // Fetch events from backend API
    const fetchEvents = async () => {
        try {
            setLoading(true);
            setError('');

            const params = new URLSearchParams();
            if (cat) params.append('category', cat);
            if (q.trim()) params.append('search', q.trim());

            const response = await fetch(`${API_BASE_URL}/events?${params}`);

            if (!response.ok) {
                throw new Error(`Failed to fetch events: ${response.status}`);
            }

            const eventsData = await response.json();
            setEvents(eventsData);
        } catch (err) {
            console.error('Error fetching events:', err);
            setError('Failed to load events. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    // Register for an event
    const handleRegisterEvent = async (eventId) => {
        if (!user) {
            navigate("/login");
            return;
        }

        try {
            const response = await fetch(`${API_BASE_URL}/events/${eventId}/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ userId: user.id })
            });

            if (!response.ok) {
                if (response.status === 409) {
                    const errorData = await response.json();
                    alert(errorData.message || 'Registration failed');
                    return;
                }
                throw new Error(`Registration failed: ${response.status}`);
            }

            const result = await response.json();
            alert(result.message || 'Successfully registered for the event!');

            // Refresh events to show updated registration count
            await fetchEvents();

        } catch (err) {
            console.error('Error registering for event:', err);
            alert('Failed to register for event. Please try again.');
        }
    };

    // Load events on component mount
    useEffect(() => {
        fetchEvents();
    }, []);

    // Debounced search - refetch when search/category changes
    useEffect(() => {
        const timeoutId = setTimeout(() => {
            fetchEvents();
        }, 500); // 500ms delay for debouncing

        return () => clearTimeout(timeoutId);
    }, [q, cat]);

    // Client-side filtering for immediate feedback while API is loading
    const filtered = useMemo(() => {
        if (loading) return events;

        const kw = q.trim().toLowerCase();
        return events.filter(ev => {
            const okCat = !cat || ev.category === cat;
            const okKw = !kw || ev.title?.toLowerCase().includes(kw) || ev.location?.toLowerCase().includes(kw);
            return okCat && okKw;
        });
    }, [events, q, cat, loading]);

    return (
        <main className="container" style={{ marginTop: 20 }}>
            <section className="hero">
                <h1>Browse events</h1>
                <div style={{ display:"flex", gap:10, marginTop:12, flexWrap:"wrap" }}>
                    <input
                        className="input"
                        placeholder="Search title or location"
                        value={q}
                        onChange={e => setQ(e.target.value)}
                        disabled={loading}
                    />
                    <select
                        className="select"
                        value={cat}
                        onChange={e => setCat(e.target.value)}
                        disabled={loading}
                    >
                        <option value="">All categories</option>
                        <option value="Tech">Tech</option>
                        <option value="Creative">Creative</option>
                        <option value="Sports">Sports</option>
                        <option value="Wellness">Wellness</option>
                    </select>
                </div>
                {loading && (
                    <div style={{ marginTop: 12, color: '#666', fontSize: '14px' }}>
                        Loading events...
                    </div>
                )}
            </section>

            {error ? (
                <div className="card" style={{
                    padding: 24,
                    textAlign: "center",
                    color: "#dc2626",
                    background: "#fef2f2",
                    border: "1px solid #fecaca"
                }}>
                    {error}
                    <br />
                    <button
                        className="btn btn-primary"
                        onClick={fetchEvents}
                        style={{ marginTop: 12 }}
                    >
                        Try Again
                    </button>
                </div>
            ) : filtered.length ? (
                <EventGrid
                    events={filtered}
                    renderCard={(ev) => (
                        <EventCard
                            key={ev.id}
                            event={ev}
                            primaryText={ev.remainingSeats <= 0 ? "Full" : "Register"}
                            onPrimary={ev.remainingSeats <= 0 ? undefined : () => handleRegisterEvent(ev.id)}
                            disabled={ev.remainingSeats <= 0}
                            // Secondary button defaults to "View details" linking to /events/:id
                        />
                    )}
                />
            ) : !loading ? (
                <div className="card" style={{ padding: 24, textAlign: "center", color: "#666" }}>
                    {q || cat ? "No events match your search criteria." : "No events available at the moment."}
                </div>
            ) : null}

            {/* Show event count */}
            {!loading && !error && events.length > 0 && (
                <div style={{
                    marginTop: 20,
                    textAlign: 'center',
                    color: '#666',
                    fontSize: '14px'
                }}>
                    Showing {filtered.length} event{filtered.length !== 1 ? 's' : ''}
                </div>
            )}
        </main>
    );
}