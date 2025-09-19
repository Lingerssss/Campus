import { Link } from "react-router-dom";

export default function EventCard({
                                      event,                 // { id, title, startAt, location, description, category, seatsLeft }
                                      onPrimary, primaryText = "Register",
                                      onSecondary, secondaryText = "View details",
                                  }) {
    const fmt = (s) => { try { return new Date(s).toLocaleString(); } catch { return s; } };

    return (
        <article className="card" style={{ padding: 20, borderRadius: 16 }}>
            {event.category && <span className="badge" style={{ marginBottom: 8 }}>{event.category}</span>}
            <h3 style={{ margin: "0 0 6px" }}>{event.title}</h3>
            <div style={{ color: "#385", fontWeight: 500 }}>
                {fmt(event.startAt)}{event.location ? ` • ${event.location}` : ""}
            </div>
            {event.description && <p style={{ color: "#555", marginTop: 8 }}>{event.description}</p>}

            <div style={{ display: "flex", gap: 12, marginTop: 12, alignItems: "center" }}>
                {onSecondary ? (
                    <button className="btn btn-ghost" onClick={() => onSecondary(event)}>
                        {secondaryText}
                    </button>
                ) : (
                    <Link className="btn btn-ghost" to={`/events/${event.id}`}>{secondaryText}</Link>
                )}
                {onPrimary && (
                    <button className="btn" onClick={() => onPrimary(event)}>{primaryText}</button>
                )}
                {typeof event.seatsLeft === "number" && (
                    <span className="pill small" title="Seats left">{event.seatsLeft} left</span>
                )}
            </div>
        </article>
    );
}
