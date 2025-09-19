import { useEffect, useMemo, useState, useCallback } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { API_BASE_URL } from "../utils/config.js";
import { useDialog } from "../components/DialogProvider";

/* ---------- datetime helpers ---------- */
// Turn ISO string from API into <input type="datetime-local"> value.
function isoToLocalInput(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return "";
    const pad = (n) => String(n).padStart(2, "0");
    const yyyy = d.getFullYear();
    const MM = pad(d.getMonth() + 1);
    const dd = pad(d.getDate());
    const hh = pad(d.getHours());
    const mm = pad(d.getMinutes());
    return `${yyyy}-${MM}-${dd}T${hh}:${mm}`;
}
// Convert input value to ISO for API.
function localInputToIso(v) {
    if (!v) return null;
    const d = new Date(v);
    return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

export default function ManageCreateEventPage() {
    const navigate = useNavigate();
    const dlg = useDialog();

    // Support BOTH styles:
    //   - /manage/create?organizerId=1&eventId=23
    //   - /manage/:userId/events/:eventId/edit (if you add it later)
    const { userId: paramUserId, eventId: paramEventId } = useParams();
    const [sp] = useSearchParams();
    // Get user ID from URL params or query string - no localStorage dependency
    const userId = paramUserId ?? sp.get("organizerId");
    const eventId = paramEventId ?? sp.get("eventId");

    const isEdit = Boolean(eventId);

    // form state
    const [title, setTitle] = useState("");
    const [start, setStart] = useState(""); // datetime-local
    const [end, setEnd] = useState("");
    const [location, setLocation] = useState("");
    const [capacity, setCapacity] = useState(50);
    const [category, setCategory] = useState("Tech");
    const [imageUrl, setImageUrl] = useState(""); // using URL or dataURL
    const [description, setDescription] = useState("");

    const [loading, setLoading] = useState(isEdit);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");

    // Keep a baseline snapshot to compute "dirty" state
    const [baseline, setBaseline] = useState(null);
    const isDirty = useMemo(() => {
        if (!baseline) return true;
        return (
            baseline.title !== title ||
            baseline.start !== start ||
            baseline.end !== end ||
            baseline.location !== location ||
            baseline.capacity !== capacity ||
            baseline.category !== category ||
            baseline.imageUrl !== imageUrl ||
            baseline.description !== description
        );
    }, [baseline, title, start, end, location, capacity, category, imageUrl, description]);

    // Load existing event on edit
    useEffect(() => {
        if (!isEdit || !userId || !eventId) return;
        let alive = true;

        (async () => {
            try {
                setLoading(true);
                setError("");

                const res = await fetch(
                    `${API_BASE_URL}/manage/organizer/${encodeURIComponent(userId)}/events/${encodeURIComponent(eventId)}`,
                    { credentials: "include", headers: { Accept: "application/json" } }
                );

                if (res.status === 401) {
                    navigate("/login", { replace: true });
                    return;
                }
                if (res.status === 403) throw new Error("You are not allowed to edit this event.");
                if (res.status === 404) throw new Error("Event not found.");
                if (!res.ok) throw new Error(`Load failed (${res.status})`);

                const ev = await res.json();
                if (!alive) return;

                setTitle(ev.title ?? "");
                setStart(isoToLocalInput(ev.startAt));
                setEnd(isoToLocalInput(ev.endAt));
                setLocation(ev.location ?? "");
                setCapacity(Number(ev.capacity ?? 50));
                setCategory(ev.category ?? "Tech");
                setImageUrl(ev.imageUrl ?? "");
                setDescription(ev.description ?? "");

                setBaseline({
                    title: ev.title ?? "",
                    start: isoToLocalInput(ev.startAt),
                    end: isoToLocalInput(ev.endAt),
                    location: ev.location ?? "",
                    capacity: Number(ev.capacity ?? 50),
                    category: ev.category ?? "Tech",
                    imageUrl: ev.imageUrl ?? "",
                    description: ev.description ?? "",
                });
            } catch (e) {
                setError(e.message || String(e));
            } finally {
                if (alive) setLoading(false);
            }
        })();

        return () => { alive = false; };
    }, [isEdit, userId, eventId, navigate]);

    // Save handler (create or update)
    const onSubmit = useCallback(async (e) => {
        e.preventDefault();
        if (!userId) return navigate("/login", { replace: true });

        const startIso = localInputToIso(start);
        const endIso   = localInputToIso(end);
        if (!startIso || !endIso) return setError("Please provide valid start/end date & time.");
        if (new Date(endIso) <= new Date(startIso)) return setError("End time must be after start time.");

        try {
            setSaving(true);
            setError("");

            const payload = {
                title,
                startAt: startIso,
                endAt: endIso,
                location,
                capacity: Number(capacity) || 0,
                category,
                imageUrl,
                description,
            };

            const url = isEdit
                ? `${API_BASE_URL}/manage/organizer/${encodeURIComponent(userId)}/events/${encodeURIComponent(eventId)}`
                : `${API_BASE_URL}/manage/organizer/${encodeURIComponent(userId)}/events`;

            const method = isEdit ? "PUT" : "POST";

            const r = await fetch(url, {
                method,
                credentials: "include",
                headers: { "Content-Type": "application/json", Accept: "application/json" },
                body: JSON.stringify(payload),
            });

            if (r.status === 401) return navigate("/login", { replace: true });
            if (r.status === 403) throw new Error("You are not allowed to modify this event.");
            if (!r.ok) throw new Error(`${method} failed (${r.status})`);

            // Stay on edit page with a toast after save change
            if (isEdit) {
                setBaseline({
                    title, start, end, location, capacity: Number(capacity) || 0,
                    category, imageUrl, description,});
                dlg.toast("Event changes saved.");
            } else {
                dlg.toast("Event created.");
                navigate(`/manage/${encodeURIComponent(userId)}`, { replace: true });
            }
        } catch (e) {
            setError(e.message || String(e));
            dlg.toast(e.message || "Save failed.", "error");
        } finally {
            setSaving(false);
        }
    }, [isEdit, userId, eventId, title, start, end, location, capacity, category, imageUrl, description, navigate]);

    // Optional: simple file->dataURL preview for image
    const onFileChange = (e) => {
        const file = e.target.files?.[0];
        if (!file) return;
        if (!file.type.startsWith("image/")) return alert("Please choose an image.");
        const reader = new FileReader();
        reader.onload = () => setImageUrl(String(reader.result));
        reader.readAsDataURL(file);
    };

    return (
        <main className="container section">
            <section className="hero" style={{ marginBottom: 16 }}>
                <h1 style={{ margin: 0 }}>{isEdit ? "Edit event" : "Create event"}</h1>
                <p className="small" style={{ marginTop: 6 }}>
                    Organizer: <code>{userId ?? "?"}</code>
                </p >
                <div style={{ marginTop: 10 }}>
                    <Link className="btn btn-ghost" to={userId ? `/manage/${encodeURIComponent(userId)}` : "/manage"}>
                        ← Back to Manage
                    </Link>
                </div>
            </section>

            <div className="card">
                {loading ? (
                    <div className="small">Loading…</div>
                ) : (
                    <form onSubmit={onSubmit} className="stack" style={{ gap: 12 }}>
                        {error && (
                            <div className="small" style={{ color: "crimson" }}>
                                Error: {error}
                            </div>
                        )}

                        <label>Title
                            <input className="input" required value={title} onChange={(e) => setTitle(e.target.value)} />
                        </label>

                        <div className="grid grid-2">
                            <label>Start
                                <input type="datetime-local" className="input" required value={start} onChange={(e) => setStart(e.target.value)} />
                            </label>
                            <label>End
                                <input type="datetime-local" className="input" required value={end} onChange={(e) => setEnd(e.target.value)} />
                            </label>
                        </div>

                        <label>Location
                            <input className="input" required value={location} onChange={(e) => setLocation(e.target.value)} />
                        </label>

                        <div className="grid grid-2">
                            <label>Capacity
                                <input type="number" min={0} className="input" value={capacity}
                                       onChange={(e) => setCapacity(Number(e.target.value))} />
                            </label>

                            <label>Category
                                <select className="select" value={category} onChange={(e) => setCategory(e.target.value)}>
                                    <option>Tech</option>
                                    <option>Creative</option>
                                    <option>Sports</option>
                                    <option>Wellness</option>
                                </select>
                            </label>
                        </div>

                        <label>Image
                            <input className="input" type="file" accept="image/*" onChange={onFileChange} />
                            {imageUrl && (
                                <div className="avatar-preview-row" style={{ marginTop: 8 }}>
                                    < img src={imageUrl} alt="preview" className="avatar-preview" />
                                    <button type="button" className="btn" onClick={() => setImageUrl("")}>Remove</button>
                                </div>
                            )}
                        </label>

                        <label>Description
                            <textarea className="textarea" rows={4} value={description} onChange={(e) => setDescription(e.target.value)} />
                        </label>

                        <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
                            <Link className="btn btn-ghost" to={userId ? `/manage/${encodeURIComponent(userId)}` : "/manage"}>Cancel</Link>
                            <button className="btn btn-primary" type="submit" disabled={saving || !isDirty}>
                                {saving ? "Saving…" : isEdit ? "Save changes" : "Create"}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </main>
    );
}