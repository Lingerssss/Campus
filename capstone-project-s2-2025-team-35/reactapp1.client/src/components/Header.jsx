import { useEffect, useState, useRef} from "react";
import { Link, useNavigate } from "react-router-dom";
import { API_BASE_URL } from "../utils/config.js";
import logo from "../assets/img/logo.png";
import defaultAvatar from "../assets/img/profile.jpg";

export default function Header() {
    const [user, setUser] = useState(null);
    const navigate = useNavigate();
    const [isMenuOpen, setIsMenuOpen] = useState(false);
    const menuRef = useRef(null);
    const [profile, setProfile] = useState(null);
    const [avatarUrl, setAvatarUrl] = useState("");

    function buildAvatarUrl(u) {
        if (!u) return "";
        if (u.startsWith("blob:")) return "";                     // never keep blob: after reload
        if (/^https?:\/\//i.test(u) || u.startsWith("data:")) return u;
        try {
            // Use WHATWG URL to resolve relative -> correct origin (no accidental “/api” in the middle)
            return new URL(u, API_BASE_URL).toString();
        } catch {
            return "";
        }
        }


    async function refreshMe() {
        try {
            const res = await fetch(`${API_BASE_URL}/auth/me`, {
                credentials: "include", // <-- send cookie
            });
            if (res.ok) {
                const me = await res.json(); // { id, name, email, role }
                setUser({
                    id: me.id,
                    name: me.name,
                    email: me.email,
                    role: (me.role ?? "").toString(),
                    ProfilePictureUrl: me.ProfilePictureUrl,
                });
            } else {
                setUser(null);
            }
        } catch {
            setUser(null);
        }
    }

      async function loadProfile(id) {
        if (!id) return;
        try {
            const res = await fetch(`${API_BASE_URL}/profile/${id}`, {
            headers: { Accept: "application/json" },
            credentials: "include",
            });
            if (!res.ok) return;
            const data = await res.json();
            setProfile(data);
            setAvatarUrl(buildAvatarUrl(data.profilePictureUrl ?? ""));
        } catch {}
        }



    useEffect(() => {
        refreshMe();

        // After Google popup posts success, refresh from cookie session
        const onMsg = (e) => {
            if (e?.data?.type === "GOOGLE_AUTH_SUCCESS") {
                refreshMe();
            } else if (e?.data?.type === "GOOGLE_AUTH_ERROR") {
                // 处理错误情况，可能需要刷新用户状态
                refreshMe();
            }
        };
        window.addEventListener("message", onMsg);
        return () => window.removeEventListener("message", onMsg);
    }, []);

      useEffect(() => {
            if (user?.id) loadProfile(user.id);
        }, [user?.id]); 

    useEffect(() => {
        function handleClickOutside(event) {
            if (menuRef.current && !menuRef.current.contains(event.target)) {
                setIsMenuOpen(false);
            }
        }

        if (isMenuOpen) {
            // Use both mousedown and click for better coverage
            document.addEventListener("mousedown", handleClickOutside);
        }

        return () => {
            document.removeEventListener("mousedown", handleClickOutside);
        };
    }, [isMenuOpen]);

    const handleLogout = async () => {
        try {
            await fetch(`${API_BASE_URL}/auth/logout`, {
                method: "POST",
                credentials: "include",
            });
        } catch {}
        setUser(null);
        setProfile(null);
        setIsMenuOpen(false);
        navigate("/login");
    };

    const handleNavigation = (path) => {
        setIsMenuOpen(false); // Close menu before navigation
        navigate(path);
    };

    const userId = user?.id;
    const dashHref = userId ? `/dashboard/${encodeURIComponent(userId)}` : "/login";
    const profileHref = userId ? `/profile/${encodeURIComponent(userId)}` : "/login";


    return (
        <header className="header">
            <div className="container nav">
                <div className="logo">
                    < img src={logo} alt="logo" />
                </div>

                <nav className="navlinks" style={{ display: "flex", gap: 8 }}>
                    <Link to="/">Home</Link>
                    <Link to="/events">Events</Link>
                    <Link to={dashHref}>Dashboard</Link>
                </nav>

                <div className="spacer" />

                {user && (
                    <div className="user-info" style={{ 
                        display: "flex", 
                        alignItems: "center", 
                        gap: "12px",
                        marginRight: "12px"
                    }}>
                        <span className="username-display" style={{
                            fontSize: "14px",
                            fontWeight: "500",
                            color: "#2851aaff",
                            whiteSpace: "nowrap"
                        }}>
                            {"Welcome, " + user.name || user.email}
                        </span>
                    </div>
                )}

                <label className="popup" aria-label="Header quick menu" ref={menuRef}>
                    <input 
                        type="checkbox" 
                        checked={isMenuOpen}
                        onChange={(e) => setIsMenuOpen(e.target.checked)}
                    />
                    <div tabIndex={0} className="burger" role="button" aria-haspopup="menu" aria-expanded={isMenuOpen}>
                        {user ? (
                            <img
                            src={avatarUrl || defaultAvatar}
                            alt={user.name ? `${user.name}'s avatar` : "User avatar"}
                            className="burger-avatar"
                            onError={(e) => { e.currentTarget.src = defaultAvatar; }}
                            style={{
                                borderRadius: "50%",
                                objectFit: "cover",
                                display: "block",
                            }}
                            />
                        ) : (
                            // optional: keep the SVG only when no user is logged in
                            <svg viewBox="0 0 24 24" fill="white" height="20" width="20" aria-hidden="true">
                            <path d="M12 2c2.757 0 5 2.243 5 5.001 0 2.756-2.243 5-5 5s-5-2.244-5-5c0-2.758 2.243-5.001 5-5.001zm0-2c-3.866 0-7 3.134-7 7.001 0 3.865 3.134 7 7 7s7-3.135 7-7c0-3.867-3.134-7.001-7-7.001zm6.369 13.353c-.497.498-1.057.931-1.658 1.302 2.872 1.874 4.378 5.083 4.972 7.346h-19.387c.572-2.29 2.058-5.503 4.973-7.358-.603-.374-1.162-.811-1.658-1.312-4.258 3.072-5.611 8.506-5.611 10.669h24c0-2.142-1.44-7.557-5.631-10.647z" />
                            </svg>
                        )}
                    </div>

                    <nav className="popup-window" role="menu">
                        <legend>Quick Start</legend>
                            {user ? (
                                    <>
                                        <ul>
                                        <li>
                                            <button onClick={() => handleNavigation(profileHref)}>
                                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                                    strokeWidth="1" strokeLinecap="round" xmlns="http://www.w3.org/2000/svg">
                                                    <path d="M2.598 9h-1.055c1.482-4.638 5.83-8 10.957-8 6.347 0 11.5 5.153 11.5 11.5s-5.153 11.5-11.5 11.5c-5.127 0-9.475-3.362-10.957-8h1.055c1.443 4.076 5.334 7 9.902 7 5.795 0 10.5-4.705 10.5-10.5s-4.705-10.5-10.5-10.5c-4.568 0-8.459 2.923-9.902 7zm12.228 3l-4.604-3.747.666-.753 6.112 5-6.101 5-.679-.737 4.608-3.763h-14.828v-1h14.826z"/>
                                                </svg>
                                                <span>Profile</span>
                                            </button>
                                        </li>
                                        <li>
                                            <button onClick={handleLogout}>
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                                strokeWidth="1" strokeLinecap="round" xmlns="http://www.w3.org/2000/svg">
                                                <path d="M2.598 9h-1.055c1.482-4.638 5.83-8 10.957-8 6.347 0 11.5 5.153 11.5 11.5s-5.153 11.5-11.5 11.5c-5.127 0-9.475-3.362-10.957-8h1.055c1.443 4.076 5.334 7 9.902 7 5.795 0 10.5-4.705 10.5-10.5s-4.705-10.5-10.5-10.5c-4.568 0-8.459 2.923-9.902 7zm12.228 3l-4.604-3.747.666-.753 6.112 5-6.101 5-.679-.737 4.608-3.763h-14.828v-1h14.826z"/>
                                            </svg>
                                            <span>Logout</span>
                                            </button>
                                        </li>
                                        </ul>
                                    </>
                                ) : (
                                    <ul>
                                    <li>
                                        <button onClick={() => navigate("/login")}>
                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                                            strokeWidth="1.2" strokeLinecap="round" xmlns="http://www.w3.org/2000/svg">
                                            <path d="M19 4v6.406l-3.753 3.741-6.463-6.462 3.7-3.685h6.516zm2-2h-12.388l1.497 1.5-4.171 4.167 9.291 9.291 4.161-4.193 1.61 1.623v-12.388zm-5 4c.552 0 1 .449 1 1s-.448 1-1 1-1-.449-1-1 .448-1 1-1zm0-1c-1.104 0-2 .896-2 2s.896 2 2 2 2-.896 2-2-.896-2-2-2zm6.708.292l-.708.708v3.097l2-2.065-1.292-1.74zm-12.675 9.294l-1.414 1.414h-2.619v2h-2v2h-2v-2.17l5.636-5.626-1.417-1.407-6.219 6.203v5h6v-2h2v-2h2l1.729-1.729-1.696-1.685z"/>
                                        </svg>
                                        <span>Log In</span>
                                        </button>
                                    </li>
                                    </ul>
                                )}
                    </nav>
                </label>
            </div>
        </header>
    );
}