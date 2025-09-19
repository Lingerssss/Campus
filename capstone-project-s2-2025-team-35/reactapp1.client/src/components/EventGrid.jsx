export default function EventGrid({ events, renderCard }) {
    return (
        <div style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
            gap: 20
        }}>
            {events.map(ev => renderCard(ev))}
        </div>
    );
}
