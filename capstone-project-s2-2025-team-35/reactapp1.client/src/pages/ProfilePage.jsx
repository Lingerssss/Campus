// src/pages/ProfilePage.jsx
import { useState, useEffect } from "react";
import { useNavigate, Link, useParams } from "react-router-dom";
import { API_BASE_URL } from "../utils/config.js";
import defaultAvatar from "../assets/img/profile.jpg"; // ✅ renamed (minimal change)

/* helpers */
function formatDate(s) {
    try {
        return new Date(s).toLocaleString(undefined, {
            year: "numeric", month: "short", day: "2-digit",
            hour: "2-digit", minute: "2-digit",
        });
    } catch { return s ?? "—"; }
}

export default function ProfilePage() {
    const { userId } = useParams();
    const navigate = useNavigate();

    const [user, setUser] = useState(null);
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    // form state
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");       // not shown as input, still mapped

    // avatar state
    const [avatarUrl, setAvatarUrl] = useState("");
    const [avatarFile, setAvatarFile] = useState(null);
    const [avatarBusy, setAvatarBusy] = useState(false);

    async function refreshMe() {
        try {
            const res = await fetch(`${API_BASE_URL}/auth/me`, { credentials: "include" });
            if (res.ok) setUser(await res.json());
            else setUser(null);
        } catch { setUser(null); }
    }

    async function loadProfile(id) {
        if (!id) {
            setError("You’re not logged in.");
            setLoading(false);
            return;
        }
        try {
            setLoading(true);
            setError("");
            const res = await fetch(`${API_BASE_URL}/profile/${id}`, {
                headers: { Accept: "application/json" },
                credentials: "include",
            });
            if (res.status === 401) {
                setUser(null);
                setError("Your session expired. Please log in.");
                setLoading(false);
                return;
            }
            if (!res.ok) throw new Error(`Failed to load profile: ${res.status}`);

            const data = await res.json();
            setProfile(data);
            setName(data.username ?? "");
            setEmail(data.email ?? "");
            setAvatarUrl(data.profilePictureUrl ?? "");
        } catch (err) {
            setError(err.message || "Failed to load profile");
        } finally {
            setLoading(false);
        }
    }

    function onAvatarPick(file) {
        if (!file) return;
        setAvatarFile(file);
        const url = URL.createObjectURL(file);
        setAvatarUrl(url);
    }

    async function saveProfile() {
        if (!user?.id) return navigate("/login", { replace: true });

        try {
            setError("");

            // 1) Upload avatar if changed
            if (avatarFile) {
                const fd = new FormData();
                fd.append("file", avatarFile);
                setAvatarBusy(true);
                const up = await fetch(`${API_BASE_URL}/profile/${user.id}/avatar`, {
                    method: "POST",
                    body: fd,
                    credentials: "include",
                });
                if (!up.ok) throw new Error(`Avatar upload failed: ${up.status}`);
                const upData = await up.json();
                setAvatarUrl(upData.avatarUrl || avatarUrl);
                setAvatarBusy(false);
            }

            // 2) Save basic fields
            const res = await fetch(`${API_BASE_URL}/profile/${user.id}`, {
                method: "PUT",
                headers: { "Content-Type": "application/json", Accept: "application/json" },
                credentials: "include",
                body: JSON.stringify({
                    username: name,
                    email, // not edited in UI, still sent
                    profilePictureUrl: avatarUrl || null,
                }),
            });

            if (res.status === 204) {
                // Re-fetch
                const getRes = await fetch(`${API_BASE_URL}/profile/${user.id}`, { credentials: "include" });
                if (getRes.ok) setProfile(await getRes.json());
            } else if (res.ok) {
                // 200 OK with updated profile
                const updated = await res.json();
                setProfile(updated);
                setName(updated.username ?? "");
                setEmail(updated.email ?? "");
                setAvatarUrl(updated.profilePictureUrl ?? "");
            } else {
                throw new Error(`Failed to save profile: ${res.status}`);
            }


            alert("Profile saved.");
        } catch (err) {
            setError(err.message || "Failed to save profile");
        } finally {
            setAvatarBusy(false);
        }
    }

    useEffect(() => {
        refreshMe();
        const onMsg = (e) => {
            if (e?.data?.type === "GOOGLE_AUTH_SUCCESS") refreshMe();
        };
        window.addEventListener("message", onMsg);
        return () => window.removeEventListener("message", onMsg);
    }, []);

    useEffect(() => {
        const id = userId ?? user?.id;
        if (id) loadProfile(id);
    }, [userId, user?.id]);

    const role = String(user?.role || profile?.role || "").toLowerCase();

    // ---------- UI (unchanged design) ----------
    return (
        <main className="container" style={{ marginTop: 20 }}>
            <section className="hero">
                <h1>My Profile</h1>
                <p>{role === "organizer" ? "Your organizer account details." : "Your student account details."}</p >
            </section>

            <div className="card" style={{ marginTop: 18, padding: 16 }}>
                {!user && !loading && (
                    <div style={{ textAlign: "center", color: "#dc2626" }}>
                        You’re not logged in.
                        <div style={{ marginTop: 12 }}>
                            <button className="btn btn-primary" onClick={() => navigate("/login")}>
                                Log in
                            </button>
                        </div>
                    </div>
                )}

                {loading && <div style={{ padding: "24px 12px", color: "#666" }}>Loading…</div>}

                {!loading && error && user && (
                    <div style={{ textAlign: "center", color: "#dc2626" }}>
                        {error}
                        <div style={{ marginTop: 12 }}>
                            <button
                                className="btn btn-primary"
                                onClick={() => (user ? loadProfile(user.id) : navigate("/login"))}
                            >
                                Try Again
                            </button>
                        </div>
                    </div>
                )}

                {!loading && !error && user && profile && (
                    <div style={{ display: "grid", gap: 16 }}>
                        {/* Avatar + pick */}
                        <div style={{ display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap" }}>
                            <div
                                style={{
                                    width: 100,
                                    height: 100,                // or use: aspectRatio: "1 / 1"
                                    borderRadius: "50%",
                                    overflow: "hidden",
                                    background: "#f3f4f6",
                                    border: "1px solid var(--edge)",
                                    flex: "0 0 auto",
                                }}
                            >
                                <img
                                    src={avatarUrl || defaultAvatar}
                                    alt="Avatar"
                                    onError={(e) => { e.currentTarget.src = defaultAvatar; }}
                                    style={{
                                        width: "100%",
                                        height: "100%",
                                        display: "block",          // prevents bottom gap
                                        objectFit: "cover",        // fill the circle
                                        objectPosition: "50% 50%", // center the crop
                                    }}
                                />
                            </div>

                            {/* compact button next to avatar */}
                            <label
                                className="btn btn-ghost"
                                title="Change avatar"
                                style={{
                                    display: "inline-flex",
                                    alignItems: "center",
                                    gap: 8,
                                    padding: "8px 12px",
                                    borderRadius: 9999,
                                    border: "1px solid var(--edge)",
                                    background: "#fff",
                                    boxShadow: "var(--shadow)",
                                    color: "#0f4c81",
                                    fontWeight: 700,
                                    lineHeight: 1,
                                    width: "auto",        // keep compact (no full width)
                                    minWidth: "unset",
                                    flex: "0 0 auto",     // don't stretch
                                    cursor: avatarBusy ? "not-allowed" : "pointer",
                                    opacity: avatarBusy ? 0.6 : 1,
                                }}
                            >
                                <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true">
                                    <path d="M4 7h3l2-2h6l2 2h3v12H4z" fill="none" stroke="currentColor" strokeWidth="1.75" />
                                    <circle cx="12" cy="13" r="4" fill="none" stroke="currentColor" strokeWidth="1.75" />
                                </svg>
                                <span>{avatarBusy ? "Uploading…" : "Choose image"}</span>
                                <input
                                    hidden
                                    type="file"
                                    accept="image/*"
                                    onChange={(e) => onAvatarPick(e.target.files?.[0])}
                                    disabled={avatarBusy}
                                />
                            </label>
                        </div>

                        {/* Editable fields */}
                        <div style={{ display: "grid", gap: 12 }}>
                            <label className="label">Name</label>
                            <input
                                className="input"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="Your name"
                            />
                        </div>

                        {/* Meta (read-only) */}
                        <table className="table" style={{ marginTop: 8 }}>
                            <tbody>
                            <tr><th>Email</th><td>{profile.email}</td></tr>
                            <tr><th style={{ width: 160 }}>ID</th><td>{profile.id}</td></tr>
                            <tr><th>Role</th><td>{profile.role}</td></tr>
                            <tr><th>Created</th><td>{formatDate(profile.createdAt)}</td></tr>
                            <tr><th>Last login</th><td>{profile.lastLoginAt ? formatDate(profile.lastLoginAt) : "—"}</td></tr>
                            </tbody>
                        </table>

                        {/* Actions */}
                        <div
                            style={{
                                marginTop: 12,
                                display: "flex",
                                gap: 12,
                                alignItems: "center",
                                justifyContent: "space-between",
                                borderTop: "1px dashed var(--edge)",
                                paddingTop: 12,
                            }}
                        >
                            <Link
                                to={`/dashboard/${user?.id}`}
                                className="btn btn-ghost"
                                style={{
                                    display: "inline-flex",
                                    alignItems: "center",
                                    gap: 8,
                                    textDecoration: "none",
                                    fontSize: 15,
                                    fontWeight: 800,
                                    color: "#0f4c81",
                                    padding: "12px 16px",
                                    borderRadius: 9999,
                                    border: "1px solid var(--edge)",
                                    background: "#fff",
                                    boxShadow: "var(--shadow)",
                                }}
                                title="Back to dashboard"
                            >
                                <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true">
                                    <path d="M15 18l-6-6 6-6" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
                                </svg>
                                <span>Go to dashboard</span>
                            </Link>

                            {(() => {
                                const noChanges =
                                    name === (profile.username ?? "") &&
                                    // email unchanged (not editable in UI)
                                    !avatarFile;

                                const isDisabled = avatarBusy || noChanges;

                                return (
                                    <span
                                        title={isDisabled ? "You haven't made any changes" : "Save your changes"}
                                        style={{ display: "inline-flex" }}
                                    >
                    <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={saveProfile}
                        disabled={isDisabled}
                        aria-disabled={isDisabled}
                        style={{
                            display: "inline-flex",
                            alignItems: "center",
                            gap: 8,
                            textDecoration: "none",
                            fontSize: 15,
                            fontWeight: 800,
                            color: isDisabled ? "#9aa3af" : "#0f4c81",
                            padding: "12px 16px",
                            borderRadius: 9999,
                            border: `1px solid ${isDisabled ? "#e5e7eb" : "var(--edge)"}`,
                            background: isDisabled ? "#fafafa" : "#fff",
                            boxShadow: isDisabled ? "none" : "var(--shadow)",
                            cursor: isDisabled ? "not-allowed" : "pointer",
                            pointerEvents: isDisabled ? "none" : "auto", // ok to keep; wrapper handles the tooltip
                            minWidth: 148,
                            justifyContent: "center",
                        }}
                    >
                      <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true">
                        <path
                            d="M20 6L9 17l-5-5"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                        />
                      </svg>
                      <span>Save changes</span>
                    </button>
                  </span>
                                );
                            })()}
                        </div>
                    </div>
                )}
            </div>
        </main>
    );
}