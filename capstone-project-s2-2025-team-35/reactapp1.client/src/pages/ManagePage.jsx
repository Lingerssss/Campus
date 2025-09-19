// ManagePage.jsx
import {
  useEffect,
  useState,
  useCallback,
  useMemo,
  useRef,
  useLayoutEffect,
} from "react";
import { createPortal } from "react-dom";
import { Link, useNavigate, useParams } from "react-router-dom";
import { API_BASE_URL } from "../utils/config.js";

/* ----------------------- tiny confirm modal (awaitable) ----------------------- */
function useConfirm() {
  const [state, setState] = useState({
    open: false,
    message: "",
    resolve: null,
  });

  const confirm = useCallback((message) => {
    return new Promise((resolve) => {
      setState({ open: true, message, resolve });
    });
  }, []);

  const close = (result) => {
    state.resolve?.(result);
    setState((s) => ({ ...s, open: false, resolve: null }));
  };

  const ui = state.open
    ? createPortal(
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Confirm"
          style={{
            position: "fixed",
            inset: 0,
            background: "rgba(0,0,0,0.38)",
            display: "grid",
            placeItems: "center",
            zIndex: 3000,
          }}
          onKeyDown={(e) => e.key === "Escape" && close(false)}
        >
          <div className="card" style={{ maxWidth: 440, padding: 16 }}>
            <h3 style={{ margin: "0 0 6px 0" }}>Please confirm</h3>
            <p style={{ margin: "0 0 16px 0" }}>{state.message}</p>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button className="btn btn-ghost" onClick={() => close(false)}>
                Cancel
              </button>
              <button className="btn btn-primary" onClick={() => close(true)}>
                OK
              </button>
            </div>
          </div>
        </div>,
        document.body
      )
    : null;

  return [confirm, ui];
}

/* ------------------------- helpers ------------------------- */

function fmtRange(start, end) {
  const s = start ? new Date(start) : null;
  const e = end ? new Date(end) : null;
  const vs = s && !isNaN(s?.getTime?.());
  const ve = e && !isNaN(e?.getTime?.());
  if (!vs && !ve) return "—";
  if (!vs) return `– ${e.toLocaleString()}`;
  if (!ve) return `${s.toLocaleString()} –`;
  return `${s.toLocaleString()} – ${e.toLocaleString()}`;
}

// Compute spots left
function getSpotsLeft(ev) {
  const cap = Number(ev?.capacity ?? 0);
  const reg = Number(ev?.registered ?? 0);
  const fromRemaining = ev?.remainingSeats;
  return typeof fromRemaining === "number" ? fromRemaining : Math.max(0, cap - reg);
}

function SpotsBadge({ event }) {
  const left = getSpotsLeft(event);
  const cap = Number(event?.capacity ?? 0);
  const critical = left <= 0;
  const warn = left > 0 && left <= 5;

  const style = {
    background: critical ? "#fee2e2" : warn ? "#fef9c3" : undefined,
    color: critical ? "#b91c1c" : warn ? "#92400e" : undefined,
    borderColor: critical ? "#fecaca" : undefined,
  };

  return (
    <span className="badge" style={style}>
      {critical ? "Full" : `${left} left`}
      {cap ? ` / ${cap}` : ""}
    </span>
  );
}

/* ---------------------- Sort dropdown (styled) ---------------------- */
function SortDropdown({ value, onChange }) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState({ top: 0, left: 0, width: 220 });
  const btnRef = useRef(null);
  const menuRef = useRef(null);
  const menuId = "sort-menu";

  const items = [
    { key: "time", label: "Start time (asc)" },
    { key: "spots", label: "Spots left (asc)" },
    { key: "created", label: "Created time (ID asc)" },
  ];
  const current = items.find((i) => i.key === value)?.label ?? "Sort";

  const updatePosition = () => {
    const btn = btnRef.current;
    if (!btn) return;
    const r = btn.getBoundingClientRect();
    const gap = 8;
    setPos({
      top: window.scrollY + r.bottom + gap,
      left: window.scrollX + r.left,
      width: Math.max(220, r.width),
    });
  };

  useLayoutEffect(() => {
    if (open) updatePosition();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onScroll = () => updatePosition();
    const onResize = () => updatePosition();
    window.addEventListener("scroll", onScroll, true);
    window.addEventListener("resize", onResize);
    window.addEventListener("orientationchange", onResize);
    return () => {
      window.removeEventListener("scroll", onScroll, true);
      window.removeEventListener("resize", onResize);
      window.removeEventListener("orientationchange", onResize);
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onDocClick = (e) => {
      if (btnRef.current?.contains(e.target) || menuRef.current?.contains(e.target))
        return;
      setOpen(false);
    };
    const onKey = (e) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onDocClick);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDocClick);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  const menu = open
    ? createPortal(
        <div
          id={menuId}
          ref={menuRef}
          role="menu"
          aria-label="Sort options"
          className="card"
          style={{
            position: "absolute",
            top: pos.top,
            left: pos.left,
            minWidth: pos.width,
            zIndex: 2000,
            padding: 6,
          }}
        >
          {items.map((it) => {
            const checked = value === it.key;
            return (
              <button
                key={it.key}
                role="menuitemradio"
                aria-checked={checked}
                className="btn btn-ghost"
                onClick={() => {
                  onChange?.(it.key);
                  setOpen(false);
                  btnRef.current?.focus();
                }}
                style={{
                  width: "100%",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  gap: 10,
                }}
              >
                <span>{it.label}</span>
                {checked && (
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
                )}
              </button>
            );
          })}
        </div>,
        document.body
      )
    : null;

  return (
    <div className="dropdown">
      <button
        ref={btnRef}
        className="dropbtn"
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={menuId}
        onClick={() => {
          setOpen((o) => !o);
          requestAnimationFrame(updatePosition);
        }}
        onKeyDown={(e) => {
          if (["ArrowDown", "Enter", " "].includes(e.key)) {
            e.preventDefault();
            setOpen(true);
            requestAnimationFrame(updatePosition);
          }
        }}
        title="Sort list"
      >
        <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true" style={{ opacity: 0.9 }}>
          <path d="M10 6h10M4 12h16M8 18h12" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
        </svg>
        <span style={{ fontWeight: 700 }}>{current}</span>
      </button>
      {menu}
    </div>
  );
}

/* ----------------------------- Manage Page ----------------------------- */

export default function ManagePage() {
  const { userId } = useParams();
  const navigate = useNavigate();

  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [deleting, setDeleting] = useState(new Set());
  const [sortBy, setSortBy] = useState("time");
  const [userRole, setUserRole] = useState(null);

  const [confirm, confirmUI] = useConfirm();

  useEffect(() => {
    if (!userId) navigate("/login", { replace: true });
  }, [userId, navigate]);

  useEffect(() => {
    if (!userId) return;

    const loadRoleThenEvents = async () => {
      try {
        setLoading(true);
        setError("");

        const r = await fetch(
          `${API_BASE_URL}/dashboard/user/${encodeURIComponent(userId)}/role`,
          { credentials: "include", headers: { Accept: "application/json" } }
        );

        if (r.status === 401) {
          navigate("/login", { replace: true });
          return;
        }
        if (!r.ok) throw new Error(`Failed to load user role (${r.status})`);

        const roleData = await r.json();
        setUserRole(roleData);

        if (roleData.role !== "Organizer") {
          navigate("/dashboard/" + userId, { replace: true });
          return;
        }

        await loadEvents();
      } catch (err) {
        setError(err.message || String(err));
      } finally {
        setLoading(false);
      }
    };

    loadRoleThenEvents();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  const loadEvents = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      if (!userId) return;

      const res = await fetch(
        `${API_BASE_URL}/manage/organizer/${encodeURIComponent(userId)}/events`,
        { credentials: "include", headers: { Accept: "application/json" } }
      );

      if (res.status === 401) {
        navigate("/login", { replace: true });
        return;
      }
      if (res.status === 403) {
        setError("You are not allowed to view these events.");
        return;
      }
      if (!res.ok) throw new Error(`GET /manage/organizer/${userId}/events failed (${res.status})`);

      const data = await res.json();
      setEvents(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err.message || String(err));
    } finally {
      setLoading(false);
    }
  }, [userId, navigate]);

  async function remove(id) {
    const ev = events.find((e) => String(e.id) === String(id));
    const ok = await confirm(`Delete "${ev?.title ?? "this event"}"? This cannot be undone.`);
    if (!ok) return;

    setDeleting((s) => new Set(s).add(id));
    try {
      const res = await fetch(
        `${API_BASE_URL}/manage/organizer/${encodeURIComponent(userId)}/events/${encodeURIComponent(id)}`,
        { method: "DELETE", credentials: "include", headers: { Accept: "application/json" } }
      );
      if (res.status === 401) {
        navigate("/login", { replace: true });
        return;
      }
      if (res.status === 403) throw new Error("You don't own this event.");
      if (res.status === 404) {
        await loadEvents();
        return;
      }
      if (!res.ok) throw new Error(`Delete failed (${res.status})`);
      setEvents((curr) => curr.filter((e) => String(e.id) !== String(id)));
    } catch (err) {
      setError(err.message || String(err));
      await loadEvents();
    } finally {
      setDeleting((s) => {
        const n = new Set(s);
        n.delete(id);
        return n;
      });
    }
  }

  const sortedEvents = useMemo(() => {
    const copy = [...events];
    if (sortBy === "spots") {
      copy.sort((a, b) => {
        const la = getSpotsLeft(a);
        const lb = getSpotsLeft(b);
        if (la !== lb) return la - lb;
        return String(a.title).localeCompare(String(b.title));
      });
    } else if (sortBy === "created") {
      copy.sort((a, b) => {
        const ia = Number.isFinite(+a?.id) ? +a.id : Number.POSITIVE_INFINITY;
        const ib = Number.isFinite(+b?.id) ? +b.id : Number.POSITIVE_INFINITY;
        if (ia !== ib) return ia - ib;
        return String(a.title).localeCompare(String(b.title));
      });
    } else {
      copy.sort((a, b) => {
        const ta = a?.startAt ? new Date(a.startAt).getTime() : Number.POSITIVE_INFINITY;
        const tb = b?.startAt ? new Date(b.startAt).getTime() : Number.POSITIVE_INFINITY;
        if (ta !== tb) return ta - tb;
        return String(a.title).localeCompare(String(b.title));
      });
    }
    return copy;
  }, [events, sortBy]);

  const stats = useMemo(() => {
    const totalEvents = events.length;
    const totalRegistrations = events.reduce(
      (sum, e) => sum + (Number(e?.registered ?? 0) || 0),
      0
    );
    return { totalEvents, totalRegistrations };
  }, [events]);

  const total = events.length;
  const shown = sortedEvents.length;

  return (
    <>
      <main className="container section">
        <section className="hero" style={{ marginBottom: 20 }}>
          <h1 style={{ margin: 0 }}>My events</h1>
          <p style={{ marginTop: 6 }}>Here are the events you’ve organized. 📅</p>
          {!loading && !error && (
            <div className="small" style={{ marginTop: 8 }}>
              {stats.totalEvents} event{stats.totalEvents !== 1 ? "s" : ""},{" "}
              {stats.totalRegistrations} total registration
              {stats.totalRegistrations !== 1 ? "s" : ""}
            </div>
          )}
        </section>

        <div className="card">
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              marginBottom: 12,
              gap: 12,
            }}
          >
            <div>
              <h2 style={{ margin: 0 }}>My events</h2>
              {!loading && !error && (
                <div className="small">Showing {shown} of {total}</div>
              )}
            </div>

            <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
              <SortDropdown value={sortBy} onChange={setSortBy} />
              <Link
                to={`/manage/create${userId ? `?organizerId=${encodeURIComponent(userId)}` : ""}`}
                className="createBtn"
              >
                Create event
              </Link>
            </div>
          </div>

          <table className="table">
            <thead>
              <tr>
                <th>Title</th>
                <th>When</th>
                <th>Location</th>
                <th>Spots</th>
                <th className="right" style={{ width: 220 }}></th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={5} className="small">Loading…</td>
                </tr>
              )}
              {!loading && error && (
                <tr>
                  <td colSpan={5} className="small" style={{ color: "crimson" }}>
                    Error: {error}
                  </td>
                </tr>
              )}
              {!loading && !error && sortedEvents.map((ev) => (
                <tr key={String(ev.id)}>
                  <td>{ev.title}</td>
                  <td>{fmtRange(ev.startAt, ev.endAt)}</td>
                  <td>{ev.location}</td>
                  <td><SpotsBadge event={ev} /></td>
                  <td style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
                    <Link
                      className="btn btn-primary"
                      to={`/manage/create?eventId=${encodeURIComponent(ev.id)}&organizerId=${encodeURIComponent(userId ?? "")}`}
                    >
                      Edit
                    </Link>
                    <button
                      className="btn btn-ghost"
                      onClick={() => remove(ev.id)}
                      disabled={deleting.has(ev.id)}
                      aria-label={`Delete ${ev.title}`}
                    >
                      {deleting.has(ev.id) ? "Deleting…" : "Delete"}
                    </button>
                  </td>
                </tr>
              ))}
              {!loading && !error && sortedEvents.length === 0 && (
                <tr>
                  <td colSpan={5} className="small">No events yet.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </main>

      {confirmUI}
    </>
  );
}
