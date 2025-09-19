// src/pages/EventPage.jsx
import React, { useEffect, useState, useCallback } from "react";
import { useParams, Link } from "react-router-dom";
import "../styles.css";
import { API_BASE_URL } from '../utils/config.js';


/** 🔧 FIXED: Clear polling timer when popup succeeds to avoid memory leaks **/
function openLoginPopup(returnUrl) {
    return new Promise((resolve, reject) => {
        const w = 520, h = 620;
        const left = window.screenX + Math.max(0, (window.outerWidth - w) / 2);
        const top  = window.screenY + Math.max(0, (window.outerHeight - h) / 2);
        const loginUrl = `${API_BASE_URL}/auth/google?returnUrl=${encodeURIComponent(returnUrl)}`;

        const popup = window.open(
            loginUrl,
            "google_oauth",
            `width=${w},height=${h},left=${left},top=${top},resizable,scrollbars`
        );
        if (!popup) return reject(new Error("Popup blocked"));

        let timer; // 🔧 ADDED: keep timer reference so we can clear it on success

        const onMsg = (ev) => {
            try {
                // Accept messages from same-origin or localhost:5173 (dev). Replace for prod.
                if (ev.origin !== window.location.origin && !ev.origin.endsWith(":5173")) return;
                if (ev.data && ev.data.type === "GOOGLE_AUTH_SUCCESS") {
                    window.removeEventListener("message", onMsg);
                    if (timer) clearInterval(timer); // 🔧 ADDED
                    resolve(ev.data.user);
                }
            } catch (_) { /* ignore */ }
        };
        window.addEventListener("message", onMsg);

        // Fallback: if popup is closed without success message, treat as cancel/failure
        timer = setInterval(() => { // 🔧 CHANGED: assign to the timer above
            if (popup.closed) {
                clearInterval(timer);
                window.removeEventListener("message", onMsg);
                reject(new Error("Login cancelled"));
            }
        }, 400);
    });
}

/** 🔧 FIXED: Handle 403 outside; on 401 open popup and retry; also check 401/403 after retry **/
async function apiFetch(url, options = {}) {
    const res = await fetch(url, { credentials: "include", ...options });

    // 🔧 NEW: handle "logged in but no permission"
    if (res.status === 403) {
        throw new Error("Forbidden");
    }

    if (res.status === 401) {
        const ret = window.location.pathname + window.location.search;

        // 🔧 CHANGED: if user cancels login, return the original 401 to the caller
        let authed = false;
        try {
            await openLoginPopup(ret);
            authed = true;
        } catch {
            return res; // user cancelled or popup failed → return original 401
        }

        if (authed) {
            const retry = await fetch(url, { credentials: "include", ...options });

            // 🔧 NEW: re-check 401/403 after retry
            if (retry.status === 401) return retry; // let caller decide how to handle still-unauthed
            if (retry.status === 403) throw new Error("Forbidden");

            return retry;
        }
    }

    return res;
}

export default function EventPage() {
    const { id } = useParams();
    const [event, setEvent] = useState(null);
    const [loading, setLoading] = useState(true);
    const [err, setErr] = useState("");

    /** 🔧 ADDED: current user (for organizer/ownership checks) **/
    const [me, setMe] = useState(null);
    const [meErr, setMeErr] = useState("");

    const load = useCallback(async () => {
        try {
            setLoading(true);
            /** 🔧 CHANGED: use apiFetch, no X-UserId header **/
            const res = await apiFetch(`${API_BASE_URL}/events/${id}`);
            if (!res.ok) throw new Error(await res.text());
            const data = await res.json();
            setEvent(data);
            setErr("");
        } catch (e) {
            setErr(e.message || "Failed to load");
        } finally {
            setLoading(false);
        }
    }, [id]);

    /** 🔧 ADDED: load current user info for role/ownership logic **/
    const loadMe = useCallback(async () => {
        try {
            const res = await apiFetch(`${API_BASE_URL}/auth/me`);
            if (!res.ok) throw new Error(await res.text());
            const data = await res.json(); // expect { id, role, ... }
            setMe(data);
            setMeErr("");
        } catch (e) {
            // Non-fatal: allow anonymous browsing or unauthenticated state
            setMe(null);
            setMeErr(e.message || "Failed to load user");
        }
    }, []);

    /** 🔧 CHANGED: fetch both event and me on mount/when id changes **/
    useEffect(() => {
        load();
        loadMe();
    }, [load, loadMe]);

    const onRegister = async () => {
        try {
            /** 🔧 CHANGED: use apiFetch; keep POST; no X-UserId **/
            const res = await apiFetch(`${API_BASE_URL}/events/${id}/register`, {
                method: "POST",
            });
            if (!res.ok) throw new Error(await res.text());
            await load(); // refresh seats / status
        } catch (e) {
            alert(e.message || "Register failed");
        }
    };

    const onUnregister = async () => {
        try {
            /** 🔧 CHANGED: use apiFetch; DELETE; no X-UserId **/
            const res = await apiFetch(`${API_BASE_URL}/events/${id}/register`, {
                method: "DELETE",
            });
            if (!res.ok) throw new Error(await res.text());
            await load();
        } catch (e) {
            alert(e.message || "Unregister failed");
        }
    };

    if (loading) return <main className="container">Loading…</main>;
    if (err) return <main className="container">❌ {err}</main>;
    if (!event) return null;

    const seatsLeft = Math.max((event.capacity ?? 0) - (event.registered ?? 0), 0);

    /** 🔧 ADDED: role/ownership derived flags **/
    const isOrganizer = me?.role === "Organizer";
    const isOwner = isOrganizer && me?.id != null && event?.organizerId === me.id;

    /**
     * 🔧 ADDED: visibility rules
     * - showEdit: only if organizer AND owner
     * - showStudentActions: only if NOT an organizer (students can register/unregister)
     * - If organizer but NOT owner: hide both Edit and Register/Unregister
     */
    const showEdit = !!isOwner;
    const showStudentActions = !isOrganizer;

    return (
        <main className="container" style={{ marginTop: 24 }}>
            <article className="card" style={{ padding: 28 }}>
                {/* Tag pill */}
                {event.category && (
                    <div className="badge" style={{ marginBottom: 10 }}>{event.category}</div>
                )}

                {/* Title */}
                <h1 style={{ margin: "10px 0 8px", fontWeight: 800, fontSize: 28 }}>
                    {event.title}
                </h1>

                {/* Meta line */}
                <p style={{ margin: "6px 0 16px", color: "var(--ink)" }}>
                    <strong>When:</strong>{" "}
                    {new Date(event.startAt).toLocaleString()} – {new Date(event.endAt).toLocaleString()}
                    <span style={{ margin: "0 8px", color: "#90a4b8" }}>•</span>
                    <strong>Where:</strong> {event.location}
                </p>

                {/* Description */}
                {event.description && (
                    <p style={{ color: "var(--ink)", marginBottom: 16 }}>{event.description}</p>
                )}

                {/* Info strip */}
                <div className="success" style={{ borderRadius: 16, padding: "12px 14px", display: "flex", alignItems: "center", gap: 10, fontWeight: 600, marginBottom: 18 }}>
                    <span role="img" aria-label="alarm">⏰</span>
                    <span><strong>Seats left:</strong> {seatsLeft}</span>
                </div>

                {/* Footer actions */}
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    {/* 🔧 CHANGED: reflect organizer/student/owner status */}
                    <span className="small">
                        {showEdit
                            ? "You are the organizer"
                            : isOrganizer
                                ? "Organizer view"
                                : "Student view"}
                    </span>

                    <div style={{ display: "flex", gap: 8 }}>
                        {/** 🔧 CHANGED: only owner organizer sees Edit; organizers (non-owner) see nothing here */}
                        {showEdit ? (
                            <Link
                                className="btn"
                                to={`/manage/create?organizerId=${encodeURIComponent(event.organizerId)}&eventId=${encodeURIComponent(event.id)}`}
                            >
                                Edit
                            </Link>
                        ) : showStudentActions ? (
                            event.isRegistered ? (
                                // Student (already registered): allow unregistration
                                <button className="btn" onClick={onUnregister}>Unregister</button>
                            ) : (
                                // Student (not registered): allow registration (disabled if no seats left)
                                <button className="btn" onClick={onRegister} disabled={seatsLeft <= 0}>
                                    Register
                                </button>
                            )
                        ) : null}

                        <Link className="btn btn-ghost" to="/events">Back to list</Link>
                    </div>
                </div>

                {/*
          🔧 CHANGED (auth-routing):
          - OAuth enabled: on 401, apiFetch opens the /auth/google popup to sign in.
            After successful login, it retries the original request; if the user cancels
            or login fails, it returns the original 401 to the caller.

          - 403 (logged in but no permission) is thrown as Error("Forbidden") in apiFetch,
            so the page can show a proper "no access" message.

          🔧 SUGGESTION (auth-context):
          - Consider an AuthContext to cache /auth/me and provide { user, role } globally.
            You can then gate the Edit button with (me.role === 'Organizer' && me.id === event.organizerId)
            for a stricter check than relying on event.canEdit alone.

          🔧 PROD NOTE:
          - The popup callback postMessage currently allows same-origin and :5173 for local dev.
            For production, switch this to your real frontend domain whitelist.

          🔒 SECURITY:
          - The frontend only controls visibility/UX. Server-side authorization must still enforce
            that only the event’s organizer can edit/manage.
        */}
            </article>
        </main>
    );
}







