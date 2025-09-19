import { BrowserRouter, Routes, Route, Link, Navigate } from "react-router-dom";
import Header from "./components/Header";
import ProfilePage from "./pages/ProfilePage.jsx";
import ManagePage from "./pages/ManagePage.jsx";
import EventPage from "./pages/EventPage.jsx";
import ManageCreateEventPage from "./pages/ManageCreateEventPage.jsx";
import Login from "./pages/Login.jsx";
import Dashboard from "./pages/Dashboard.jsx";
import Home from "./pages/Home.jsx";
import "./styles.css";

export default function App() {
  return (
    <BrowserRouter>
      <Header />
      <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/events" element={<Home />} />
        <Route path="/profile/:uerId" element={<ProfilePage />} />
        <Route path="/manage/:userId" element={<ManagePage />} />
          <Route path="/events/:id" element={<EventPage />} />
        <Route path="/manage/create" element={<ManageCreateEventPage />} />
          <Route path="/login" element={<Login />} />
          <Route path="/dashboard/:userId" element={<Dashboard />} />
        <Route path="*" element={<div style={{ padding: 12 }}>Not found</div>} />
      </Routes>
    </BrowserRouter>
  );
}