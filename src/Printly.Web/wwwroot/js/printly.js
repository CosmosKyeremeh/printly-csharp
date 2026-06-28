// ── JWT helpers ─────────────────────────────────────────────────────────────
// We store the JWT in sessionStorage after login.
// sessionStorage is cleared when the browser tab is closed.
// Never store JWTs in localStorage — they persist and are vulnerable to XSS.

function getToken() {
    return sessionStorage.getItem("printly_token");
}

function setToken(token) {
    sessionStorage.setItem("printly_token", token);
}

function clearToken() {
    sessionStorage.removeItem("printly_token");
}

// ── API helper ───────────────────────────────────────────────────────────────
// Every API call includes the JWT in the Authorization header.
// This function wraps fetch() so we never forget to add it.
async function apiCall(url, method = "GET", body = null) {
    const headers = { "Content-Type": "application/json" };
    const token = getToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;

    const options = { method, headers };
    if (body) options.body = JSON.stringify(body);

    const response = await fetch(url, options);

    if (response.status === 401) {
        // Token expired or invalid — redirect to login
        clearToken();
        window.location.href = "/auth/login";
        return null;
    }

    return response;
}

// ── SignalR notification bell ────────────────────────────────────────────────
// Only connect if the user is logged in (token exists)
const token = getToken();
if (token) {
    // Build a SignalR connection to our hub.
    // withUrl passes the JWT as a query parameter (?access_token=...)
    // because WebSocket connections cannot set HTTP headers.
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications", {
            accessTokenFactory: () => getToken()
        })
        .withAutomaticReconnect()  // reconnect if network drops
        .build();

    // Listen for "ReceiveNotification" events pushed by the server.
    // This matches the event name we used in NotificationService.
    connection.on("ReceiveNotification", (data) => {
        incrementBell();
        showToast(data.title, data.message, data.type);
    });

    connection.start().catch(err => console.error("SignalR error:", err));
}

// ── Bell badge ───────────────────────────────────────────────────────────────
function incrementBell() {
    const badge = document.getElementById("notifCount");
    if (!badge) return;

    const current = parseInt(badge.textContent) || 0;
    badge.textContent = current + 1;
    badge.classList.remove("d-none");

    // Pulse animation
    const bell = document.getElementById("notifBell");
    bell?.classList.add("bell-pulse");
    setTimeout(() => bell?.classList.remove("bell-pulse"), 400);
}

async function loadUnreadCount() {
    const badge = document.getElementById("notifCount");
    if (!badge || !getToken()) return;

    const res = await apiCall("/api/notifications/unread-count");
    if (!res) return;

    const count = await res.json();
    if (count > 0) {
        badge.textContent = count;
        badge.classList.remove("d-none");
    }
}

// ── Toast notifications ──────────────────────────────────────────────────────
function showToast(title, message, type) {
    const container = document.getElementById("toastContainer")
        || createToastContainer();

    const colors = {
        Deadline: "warning",
        PaymentReceived: "success",
        PrintReady: "info",
        General: "secondary"
    };
    const color = colors[type] || "secondary";

    const toast = document.createElement("div");
    toast.className = `toast align-items-center text-bg-${color} border-0`;
    toast.setAttribute("role", "alert");
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <strong>${title}</strong><br>${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto"
                    data-bs-dismiss="toast"></button>
        </div>`;

    container.appendChild(toast);
    new bootstrap.Toast(toast, { delay: 5000 }).show();
    toast.addEventListener("hidden.bs.toast", () => toast.remove());
}

function createToastContainer() {
    const div = document.createElement("div");
    div.id = "toastContainer";
    div.className = "toast-container position-fixed bottom-0 end-0 p-3";
    div.style.zIndex = 1100;
    document.body.appendChild(div);
    return div;
}

// Load bell count on every page load
document.addEventListener("DOMContentLoaded", loadUnreadCount);
