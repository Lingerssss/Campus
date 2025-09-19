import { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { API_BASE_URL } from '../utils/config.js';

// format date strings
function formatDate(dateString) {
    try {
        return new Date(dateString).toLocaleString(undefined, {
            year: "numeric",
            month: "short",
            day: "2-digit",
            hour: "2-digit",
            minute: "2-digit",
        });
    } catch {
        return dateString;
    }
}

export default function Dashboard() {
    const { userId } = useParams();
    const [userRole, setUserRole] = useState(null);
    const [dashboardData, setDashboardData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const navigate = useNavigate();

    // redirect if missing param
    useEffect(() => {
        if (!userId) {
            navigate('/login', { replace: true });
        }
    }, [userId, navigate]);

    // Load role → if Organizer redirect to /manage. Else load student dashboard.
    useEffect(() => {
        if (!userId) return;

        const loadUserRole = async () => {
            try {
                setLoading(true);
                setError('');

                const response = await fetch(
                    `${API_BASE_URL}/dashboard/user/${encodeURIComponent(userId)}/role`,
                    { credentials: 'include' } // ▶ CHANGED: send cookies
                );

                if (!response.ok) {
                    if (response.status === 404) setError('User not found');
                    else throw new Error(`Failed to load user role: ${response.status}`);
                    return;
                }

                const roleData = await response.json();
                setUserRole(roleData);

                // ▶ CHANGED: organizers are sent to Manage; stop rendering this page
                if (roleData.role === 'Organizer') {
                    navigate(`/manage/${userId}`, { replace: true });
                    return;
                }

                // Students stay here
                await loadStudentDashboard(userId);
            } catch (err) {
                console.error('Failed to load user role:', err);
                setError('Failed to load user information');
                setLoading(false);
            }
        };

        loadUserRole();
    }, [userId, navigate]);

    const loadStudentDashboard = async (studentId) => {
        try {
            const response = await fetch(
                `${API_BASE_URL}/dashboard/student/${encodeURIComponent(studentId)}`,
                { credentials: 'include' } // ▶ CHANGED
            );

            if (!response.ok) {
                throw new Error(`Failed to load student dashboard: ${response.status}`);
            }

            const data = await response.json();
            setDashboardData(data);
        } catch (err) {
            console.error('Failed to load student dashboard:', err);
            setError('Failed to load your registered events');
        } finally {
            setLoading(false);
        }
    };

    const handleWithdraw = async (eventId) => {
        if (!userId || !userRole || userRole.role !== 'Student') return;

        try {
            const response = await fetch(
                `${API_BASE_URL}/dashboard/student/${encodeURIComponent(userId)}/events/${encodeURIComponent(eventId)}`,
                { method: 'DELETE', credentials: 'include' } // ▶ CHANGED
            );

            if (!response.ok) {
                throw new Error(`Failed to withdraw: ${response.status}`);
            }

            await loadStudentDashboard(userId);
            alert('Successfully withdrew from the event');
        } catch (err) {
            console.error('Failed to withdraw:', err);
            alert('Failed to withdraw from event. Please try again.');
        }
    };

    // ▶ REMOVED: organizer-specific loader and delete action
    // const loadOrganizerDashboard = ...
    // const handleDeleteEvent = ...

    if (!userId) {
        return (
            <main className="container" style={{ marginTop: '20px' }}>
                <div>Redirecting to login...</div>
            </main>
        );
    }

    const isStudent = userRole?.role === 'Student';
    // ▶ CHANGED: only student events are relevant now
    const events = isStudent ? (dashboardData?.registeredEvents || []) : [];

    return (
        <main className="container" style={{ marginTop: "20px" }}>
            <section className="hero">
                <h1>My registrations</h1> {/* ▶ CHANGED: organizer view removed */}
                <p>Here's what you're attending soon. ✨</p >

                {userRole && (
                    <div style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.8)', marginTop: '8px' }}>
                        Welcome back, {userRole.username} ({userRole.role})
                    </div>
                )}
                {dashboardData && (
                    <div style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.8)', marginTop: '4px' }}>
                        {/* ▶ CHANGED: only student stats */}
                        Total registrations: {dashboardData.totalRegistrations}
                    </div>
                )}
            </section>

            <div className="card" style={{ marginTop: "18px" }}>
                {loading ? (
                    <div style={{ padding: "40px 20px", textAlign: "center", color: "#666" }}>
                        Loading your dashboard...
                    </div>
                ) : error ? (
                    <div style={{ padding: "40px 20px", textAlign: "center", color: "#9ca3af" }}>
                        {error}
                        <br />
                        <button
                            className="btn btn-primary"
                            onClick={() => {
                                // re-try role + student fetch
                                setError('');
                                setLoading(true);
                                // trigger effect again
                                navigate(`/dashboard/${userId}`, { replace: true });
                            }}
                            style={{ marginTop: 12 }}
                        >
                            Try Again
                        </button>
                    </div>
                ) : events.length > 0 ? (
                    <div
                        style={{
                            display: "grid",
                            gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
                            gap: "20px",
                            marginTop: "16px",
                            padding: "8px"
                        }}
                    >
                        {events.map((event) => (
                            <article key={event.id} className="card" style={{ padding: "20px", borderRadius: "16px" }}>
                                <h3 style={{ margin: "0 0 6px" }}>{event.title}</h3>
                                <div style={{ color: "#385", fontWeight: 500 }}>
                                    {formatDate(event.startAt)}
                                    {event.location ? ` • ${event.location}` : ""}
                                </div>

                                {event.description && (
                                    <p style={{ color: "#555", marginTop: "8px" }}>{event.description}</p >
                                )}

                                {event.organizerName && (
                                    <div style={{ fontSize: "12px", color: "#666", marginTop: "4px" }}>
                                        Organized by: {event.organizerName}
                                    </div>
                                )}

                                <div style={{ display: "flex", gap: "12px", marginTop: "12px", alignItems: "center" }}>
                                    <Link to={`/events/${event.id}`} className="btn btn-ghost">
                                        View details
                                    </Link>
                                    {/* ▶ CHANGED: only withdraw button */}
                                    <button className="btn" onClick={() => handleWithdraw(event.id)}>
                                        Withdraw
                                    </button>
                                </div>
                            </article>
                        ))}
                    </div>
                ) : (
                    <div style={{ padding: "40px 20px", textAlign: "center", color: "#666" }}>
                        You haven't registered for any events yet.
                        <br />
                        <Link to="/events" className="btn btn-primary" style={{ marginTop: '12px' }}>
                            Browse Events
                        </Link>
                    </div>
                )}
            </div>
        </main>
    );
}