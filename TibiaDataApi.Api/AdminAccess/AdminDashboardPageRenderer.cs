namespace TibiaDataApi.AdminAccess
{
    internal static class AdminDashboardPageRenderer
    {
        public static string Render()
        {
            return $$"""
                     <!DOCTYPE html>
                     <html lang="en">
                     <head>
                         <meta charset="utf-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1">
                         <title>{{AdminAccessDefaults.AdminDashboardTitle}}</title>
                         <style>
                             :root {
                                 color-scheme: light;
                                 --bg: #eaf2ff;
                                 --panel: rgba(249, 252, 255, 0.94);
                                 --panel-strong: #ffffff;
                                 --panel-muted: #edf4ff;
                                 --line: rgba(53, 106, 180, 0.18);
                                 --text: #132033;
                                 --muted: #58708f;
                                 --accent: #2563eb;
                                 --accent-strong: #1d4ed8;
                                 --accent-soft: rgba(37, 99, 235, 0.12);
                                 --good: #256d3d;
                                 --warn: #a45f15;
                                 --danger: #a63125;
                                 --shadow: 0 24px 70px rgba(25, 55, 102, 0.16);
                                 --body-grad-a: rgba(37, 99, 235, 0.24);
                                 --body-grad-b: rgba(14, 165, 233, 0.18);
                                 --body-grid: rgba(37, 99, 235, 0.04);
                                 --button-ghost-bg: rgba(255, 255, 255, 0.76);
                                 --button-ghost-border: rgba(53, 106, 180, 0.18);
                                 --table-hover: rgba(37, 99, 235, 0.06);
                                 --empty-bg: rgba(255, 255, 255, 0.55);
                                 --empty-border: rgba(53, 106, 180, 0.18);
                             }

                             body[data-theme="dark"] {
                                 color-scheme: dark;
                                 --bg: #09111d;
                                 --panel: rgba(10, 19, 34, 0.94);
                                 --panel-strong: rgba(14, 25, 42, 0.98);
                                 --panel-muted: rgba(18, 31, 51, 0.96);
                                 --line: rgba(120, 161, 231, 0.16);
                                 --text: #f3f7ff;
                                 --muted: #bfd0ea;
                                 --accent: #60a5fa;
                                 --accent-strong: #2563eb;
                                 --accent-soft: rgba(96, 165, 250, 0.14);
                                 --good: #4ade80;
                                 --warn: #fbbf24;
                                 --danger: #f87171;
                                 --shadow: 0 24px 70px rgba(2, 6, 23, 0.45);
                                 --body-grad-a: rgba(37, 99, 235, 0.2);
                                 --body-grad-b: rgba(14, 165, 233, 0.12);
                                 --body-grid: transparent;
                                 --button-ghost-bg: rgba(16, 29, 47, 0.92);
                                 --button-ghost-border: rgba(120, 161, 231, 0.14);
                                 --table-hover: rgba(96, 165, 250, 0.08);
                                 --empty-bg: rgba(11, 20, 34, 0.84);
                                 --empty-border: rgba(120, 161, 231, 0.14);
                             }

                             * {
                                 box-sizing: border-box;
                             }

                             body {
                                 margin: 0;
                                 min-height: 100vh;
                                 color: var(--text);
                                 font-family: "Trebuchet MS", "Segoe UI", sans-serif;
                                 background:
                                     radial-gradient(circle at top left, var(--body-grad-a), transparent 32%),
                                     radial-gradient(circle at top right, var(--body-grad-b), transparent 26%),
                                     linear-gradient(160deg, var(--bg) 0%, color-mix(in srgb, var(--bg) 84%, #b9d4ff) 48%, color-mix(in srgb, var(--bg) 92%, #ffffff) 100%);
                             }

                             body::before {
                                 content: "";
                                 position: fixed;
                                 inset: 0;
                                 pointer-events: none;
                                 background:
                                     linear-gradient(rgba(255,255,255,0.22), rgba(255,255,255,0)),
                                     repeating-linear-gradient(
                                         90deg,
                                         var(--body-grid) 0,
                                         var(--body-grid) 1px,
                                         transparent 1px,
                                         transparent 24px
                                     );
                             }

                             body[data-theme="dark"]::before {
                                 background: none;
                             }

                             a {
                                 color: inherit;
                             }

                             .shell {
                                 width: min(1440px, calc(100vw - 32px));
                                 margin: 0 auto;
                                 padding: 24px 0 40px;
                             }

                             .hero {
                                 position: relative;
                                 overflow: hidden;
                                 padding: 28px;
                                 border: 1px solid var(--line);
                                 border-radius: 28px;
                                 background:
                                     linear-gradient(140deg, color-mix(in srgb, var(--panel-strong) 92%, #ffffff), color-mix(in srgb, var(--panel-muted) 96%, #dbeafe)),
                                     var(--panel);
                                 box-shadow: var(--shadow);
                             }

                             .hero::after {
                                 content: "";
                                 position: absolute;
                                 inset: auto -8% -28% auto;
                                 width: 240px;
                                 height: 240px;
                                 border-radius: 999px;
                                 background: radial-gradient(circle, rgba(141, 77, 31, 0.22), transparent 70%);
                                 background: radial-gradient(circle, var(--accent-soft), transparent 70%);
                             }

                             body[data-theme="dark"] .hero {
                                 background:
                                     linear-gradient(140deg, rgba(16, 30, 50, 0.98), rgba(10, 20, 36, 0.96)),
                                     var(--panel);
                             }

                             .hero-top {
                                 display: flex;
                                 gap: 16px;
                                 align-items: flex-start;
                                 justify-content: space-between;
                                 flex-wrap: wrap;
                             }

                             .eyebrow {
                                 display: inline-flex;
                                 align-items: center;
                                 gap: 8px;
                                 padding: 6px 12px;
                                 border-radius: 999px;
                                 border: 1px solid var(--button-ghost-border);
                                 background: rgba(255, 255, 255, 0.62);
                                 color: var(--muted);
                                 font-size: 0.82rem;
                                 letter-spacing: 0.08em;
                                 text-transform: uppercase;
                             }

                             body[data-theme="dark"] .eyebrow {
                                 background: rgba(29, 44, 69, 0.92);
                                 color: #d6e2f7;
                             }

                             h1 {
                                 margin: 14px 0 8px;
                                 font-family: "Palatino Linotype", "Book Antiqua", Georgia, serif;
                                 font-size: clamp(2rem, 4vw, 3.2rem);
                                 line-height: 1.05;
                             }

                             .hero p {
                                 max-width: 760px;
                                 margin: 0;
                                 color: var(--muted);
                                 line-height: 1.6;
                                 font-size: 1rem;
                             }

                             .hero-actions {
                                 display: flex;
                                 gap: 10px;
                                 flex-wrap: wrap;
                             }

                             .status-pill,
                             .ghost-button,
                             .solid-button,
                             .tab-button {
                                 border-radius: 999px;
                                 font-weight: 700;
                             }

                             .status-pill {
                                 display: inline-flex;
                                 align-items: center;
                                 gap: 10px;
                                 padding: 12px 16px;
                                 background: rgba(255, 255, 255, 0.78);
                                 border: 1px solid var(--button-ghost-border);
                                 box-shadow: 0 10px 24px rgba(60, 38, 20, 0.08);
                             }

                             body[data-theme="dark"] .status-pill {
                                 background: rgba(21, 34, 55, 0.96);
                                 color: var(--text);
                                 box-shadow: 0 10px 24px rgba(2, 6, 23, 0.28);
                             }

                             .status-pill strong {
                                 display: block;
                                 font-size: 0.78rem;
                                 color: var(--muted);
                                 text-transform: uppercase;
                                 letter-spacing: 0.08em;
                             }

                             .status-dot {
                                 width: 10px;
                                 height: 10px;
                                 border-radius: 999px;
                                 background: var(--good);
                                 box-shadow: 0 0 0 6px rgba(37, 109, 61, 0.13);
                             }

                             .status-dot.warn {
                                 background: var(--warn);
                                 box-shadow: 0 0 0 6px rgba(164, 95, 21, 0.14);
                             }

                             .status-dot.danger {
                                 background: var(--danger);
                                 box-shadow: 0 0 0 6px rgba(166, 49, 37, 0.14);
                             }

                             .ghost-button,
                             .solid-button,
                             button,
                             input,
                             select {
                                 font: inherit;
                             }

                             .ghost-button,
                             .solid-button {
                                 display: inline-flex;
                                 align-items: center;
                                 justify-content: center;
                                 min-height: 44px;
                                 padding: 0 18px;
                                 border: 1px solid var(--button-ghost-border);
                                 text-decoration: none;
                                 cursor: pointer;
                             }

                             .ghost-button {
                                 background: var(--button-ghost-bg);
                             }

                             body[data-theme="dark"] .ghost-button,
                             body[data-theme="dark"] .tab-button,
                             body[data-theme="dark"] .table-actions button {
                                 color: #dbe7fb;
                             }

                             .solid-button {
                                 color: #fff9f3;
                                 background: linear-gradient(135deg, var(--accent), var(--accent-strong));
                                 border-color: transparent;
                             }

                             .grid {
                                 display: grid;
                                 gap: 18px;
                                 margin-top: 18px;
                             }

                             .stats-grid {
                                 grid-template-columns: repeat(4, minmax(0, 1fr));
                             }

                             .two-up {
                                 grid-template-columns: repeat(2, minmax(0, 1fr));
                             }

                             .panel {
                                 padding: 22px;
                                 border: 1px solid var(--line);
                                 border-radius: 22px;
                                 background: var(--panel);
                                 box-shadow: 0 14px 32px rgba(60, 38, 20, 0.08);
                                 animation: fade-up 320ms ease;
                             }

                             .panel h2,
                             .panel h3 {
                                 margin: 0 0 12px;
                                 font-family: "Palatino Linotype", "Book Antiqua", Georgia, serif;
                             }

                             .subtle {
                                 color: var(--muted);
                             }

                             .metric-card {
                                 position: relative;
                                 overflow: hidden;
                                 padding: 20px;
                                 border-radius: 22px;
                                 border: 1px solid var(--button-ghost-border);
                                 background:
                                     linear-gradient(160deg, rgba(255,255,255,0.76), color-mix(in srgb, var(--panel-muted) 92%, #ffffff)),
                                     var(--panel-strong);
                             }

                             body[data-theme="dark"] .metric-card {
                                 background:
                                     linear-gradient(160deg, rgba(22, 37, 60, 0.98), rgba(14, 25, 42, 0.98)),
                                     var(--panel-strong);
                             }

                             .metric-card::after {
                                 content: "";
                                 position: absolute;
                                 inset: auto -28px -42px auto;
                                 width: 100px;
                                 height: 100px;
                                 border-radius: 999px;
                                 background: radial-gradient(circle, var(--accent-soft), transparent 72%);
                             }

                             .metric-card .label {
                                 font-size: 0.78rem;
                                 text-transform: uppercase;
                                 letter-spacing: 0.08em;
                                 color: var(--muted);
                             }

                             body[data-theme="dark"] .metric-card .label {
                                 color: #c7d6ec;
                             }

                             .metric-card .value {
                                 margin-top: 12px;
                                 font-size: clamp(1.8rem, 3vw, 2.5rem);
                                 font-weight: 800;
                                 line-height: 1;
                             }

                             .metric-card .hint {
                                 margin-top: 8px;
                                 font-size: 0.92rem;
                                 color: var(--muted);
                             }

                             body[data-theme="dark"] .metric-card .hint,
                             body[data-theme="dark"] .panel .subtle,
                             body[data-theme="dark"] th,
                             body[data-theme="dark"] .field label {
                                 color: #c1d0e6;
                             }

                             .tabs {
                                 display: flex;
                                 gap: 10px;
                                 flex-wrap: wrap;
                                 margin-top: 18px;
                             }

                             .tab-button {
                                 min-height: 42px;
                                 padding: 0 16px;
                                 border: 1px solid var(--button-ghost-border);
                                 background: var(--button-ghost-bg);
                                 color: var(--muted);
                                 cursor: pointer;
                             }

                             .tab-button.active {
                                 color: #fff9f3;
                                 background: linear-gradient(135deg, var(--accent), var(--accent-strong));
                                 border-color: transparent;
                             }

                             .tab-panel {
                                 display: none;
                                 margin-top: 18px;
                             }

                             .tab-panel.active {
                                 display: block;
                             }

                             .stack {
                                 display: grid;
                                 gap: 18px;
                             }

                             .form-grid {
                                 display: grid;
                                 gap: 14px;
                                 grid-template-columns: repeat(4, minmax(0, 1fr));
                             }

                             .field {
                                 display: grid;
                                 gap: 8px;
                             }

                             .field.span-2 {
                                 grid-column: span 2;
                             }

                             .field label {
                                 font-size: 0.86rem;
                                 font-weight: 700;
                                 color: var(--muted);
                                 text-transform: uppercase;
                                 letter-spacing: 0.06em;
                             }

                             input[type="text"],
                             input[type="datetime-local"],
                             select {
                                 width: 100%;
                                 min-height: 46px;
                                 padding: 0 14px;
                                 border-radius: 14px;
                                 border: 1px solid var(--button-ghost-border);
                                 background: color-mix(in srgb, var(--panel-strong) 88%, #ffffff);
                                 color: var(--text);
                             }

                             body[data-theme="dark"] input[type="text"],
                             body[data-theme="dark"] input[type="datetime-local"],
                             body[data-theme="dark"] select {
                                 background: rgba(15, 28, 46, 0.96);
                                 border-color: rgba(120, 161, 231, 0.14);
                                 color: #edf3ff;
                             }

                             .checkbox-row {
                                 display: flex;
                                 align-items: center;
                                 gap: 10px;
                                 min-height: 46px;
                             }

                             .inline-actions {
                                 display: flex;
                                 gap: 10px;
                                 flex-wrap: wrap;
                                 align-items: center;
                             }

                             .message {
                                 display: none;
                                 margin-top: 18px;
                                 padding: 14px 16px;
                                 border-radius: 16px;
                                 border: 1px solid transparent;
                             }

                             .message.visible {
                                 display: block;
                             }

                             .message.info {
                                 background: rgba(64, 111, 170, 0.12);
                                 border-color: rgba(64, 111, 170, 0.24);
                             }

                             .message.success {
                                 background: rgba(37, 109, 61, 0.12);
                                 border-color: rgba(37, 109, 61, 0.22);
                             }

                             .message.error {
                                 background: rgba(166, 49, 37, 0.12);
                                 border-color: rgba(166, 49, 37, 0.22);
                             }

                             table {
                                 width: 100%;
                                 border-collapse: collapse;
                             }

                             .table-scroll {
                                 overflow: auto;
                                 border: 1px solid var(--line);
                                 border-radius: 18px;
                                 background: color-mix(in srgb, var(--panel-strong) 92%, transparent);
                             }

                             .table-scroll.metrics-scroll {
                                 max-height: 520px;
                                 overscroll-behavior: contain;
                             }

                             .table-scroll.raw-scroll {
                                 max-height: 520px;
                             }

                             .table-scroll.request-log-scroll {
                                 max-height: 560px;
                                 overscroll-behavior: contain;
                             }

                             .request-log-table {
                                 min-width: 1180px;
                             }

                             .request-log-table th {
                                 position: sticky;
                                 top: 0;
                                 z-index: 1;
                                 background: color-mix(in srgb, var(--panel-strong) 96%, transparent);
                             }

                             .metrics-table {
                                 min-width: 920px;
                                 table-layout: fixed;
                             }

                             .metrics-table th {
                                 position: sticky;
                                 top: 0;
                                 z-index: 1;
                                 background: color-mix(in srgb, var(--panel-strong) 96%, transparent);
                             }

                             .metrics-table th:nth-child(1),
                             .metrics-table td:nth-child(1) {
                                 width: 40%;
                             }

                             .metrics-table th:nth-child(2),
                             .metrics-table td:nth-child(2) {
                                 width: 14%;
                             }

                             .metrics-table th:nth-child(3),
                             .metrics-table td:nth-child(3) {
                                 width: 46%;
                             }

                             .metrics-name,
                             .metrics-help,
                             .metrics-labels {
                                 word-break: break-word;
                                 overflow-wrap: anywhere;
                             }

                             .metrics-labels {
                                 font-family: "Consolas", "Courier New", monospace;
                                 font-size: 0.84rem;
                                 line-height: 1.5;
                             }

                             body[data-theme="dark"] .table-scroll,
                             body[data-theme="dark"] .metrics-table th,
                             body[data-theme="dark"] .request-log-table th {
                                 background: rgba(11, 20, 34, 0.92);
                             }

                             th,
                             td {
                                 padding: 12px 10px;
                                 border-bottom: 1px solid var(--button-ghost-border);
                                 text-align: left;
                                 vertical-align: top;
                                 font-size: 0.94rem;
                             }

                             th {
                                 font-size: 0.78rem;
                                 text-transform: uppercase;
                                 letter-spacing: 0.08em;
                                 color: var(--muted);
                             }

                             tbody tr:hover {
                                 background: var(--table-hover);
                             }

                             .mono {
                                 font-family: "Consolas", "Courier New", monospace;
                                 font-size: 0.92rem;
                             }

                             .status-chip {
                                 display: inline-flex;
                                 align-items: center;
                                 padding: 6px 10px;
                                 border-radius: 999px;
                                 font-size: 0.82rem;
                                 font-weight: 700;
                                 background: color-mix(in srgb, var(--panel-muted) 88%, transparent);
                             }

                             .status-chip.running,
                             .status-chip.completed,
                             .status-chip.banned {
                                 color: var(--good);
                                 background: rgba(37, 109, 61, 0.14);
                             }

                             .status-chip.failed,
                             .status-chip.blocked {
                                 color: var(--danger);
                                 background: rgba(166, 49, 37, 0.14);
                             }

                             .status-chip.cancelled,
                             .status-chip.cancellationrequested,
                             .status-chip.warning {
                                 color: var(--warn);
                                 background: rgba(164, 95, 21, 0.15);
                             }

                             .empty-state {
                                 padding: 24px;
                                 border: 1px dashed var(--empty-border);
                                 border-radius: 18px;
                                 background: var(--empty-bg);
                                 color: var(--muted);
                                 text-align: center;
                             }

                             .table-actions button {
                                 min-height: 34px;
                                 padding: 0 12px;
                                 border-radius: 999px;
                                 border: 1px solid var(--button-ghost-border);
                                 background: var(--button-ghost-bg);
                                 cursor: pointer;
                             }

                             .theme-toggle {
                                 min-width: 144px;
                             }

                             @keyframes fade-up {
                                 from {
                                     opacity: 0;
                                     transform: translateY(10px);
                                 }

                                 to {
                                     opacity: 1;
                                     transform: translateY(0);
                                 }
                             }

                             @media (max-width: 1100px) {
                                 .stats-grid,
                                 .two-up,
                                 .form-grid {
                                     grid-template-columns: repeat(2, minmax(0, 1fr));
                                 }
                             }

                             @media (max-width: 720px) {
                                 .shell {
                                     width: min(100vw - 18px, 100%);
                                     padding-top: 12px;
                                 }

                                 .hero,
                                 .panel {
                                     border-radius: 20px;
                                     padding: 18px;
                                 }

                                 .stats-grid,
                                 .two-up,
                                 .form-grid {
                                     grid-template-columns: 1fr;
                                 }

                                 .field.span-2 {
                                     grid-column: span 1;
                                 }

                                 th:nth-child(5),
                                 td:nth-child(5),
                                 th:nth-child(6),
                                 td:nth-child(6) {
                                     display: none;
                                 }
                             }
                         </style>
                     </head>
                     <body>
                         <div class="shell">
                             <section class="hero">
                                 <div class="hero-top">
                                     <div>
                                         <span class="eyebrow">{{AdminAccessDefaults.AdminOperationsTitle}}</span>
                                         <h1>Admin Dashboard</h1>
                                         <p>
                                             Monitor scraper runs, inspect live API traffic, review IP activity and manage bans
                                             from one protected control surface.
                                         </p>
                                     </div>
                                     <div class="hero-actions">
                                         <div class="status-pill">
                                             <span id="hero-status-dot" class="status-dot warn"></span>
                                             <div>
                                                 <strong>Runtime</strong>
                                                 <span id="hero-status-text">Loading...</span>
                                             </div>
                                         </div>
                                         <button id="theme-toggle-button" type="button" class="ghost-button theme-toggle">Dark Theme</button>
                                         <a class="ghost-button" href="{{AdminAccessDefaults.AdminDashboardPath}}">Dashboard</a>
                                         <a class="ghost-button" href="{{AdminAccessDefaults.AdminScalarPath}}">Open Scalar Admin API</a>
                                         <a class="ghost-button" href="/">Open Public Docs</a>
                                         <a class="solid-button" href="{{AdminAccessDefaults.LogoutPath}}">Logout</a>
                                     </div>
                                 </div>
                                 <div id="message" class="message"></div>
                             </section>

                             <section class="grid stats-grid" aria-label="24 hour usage metrics">
                                 <article class="metric-card">
                                     <div class="label">Requests / 24h</div>
                                     <div id="metric-total-requests" class="value">-</div>
                                     <div class="hint">Tracked API traffic in the last 24 hours.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Unique IPs / 24h</div>
                                     <div id="metric-unique-ips" class="value">-</div>
                                     <div class="hint">Unique clients seen by the API.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Errors / 24h</div>
                                     <div id="metric-errors" class="value">-</div>
                                     <div class="hint">Requests with status code 400 or higher.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Avg Response</div>
                                     <div id="metric-avg-response" class="value">-</div>
                                     <div class="hint">Average response time in milliseconds.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Blocked / 24h</div>
                                     <div id="metric-blocked" class="value">-</div>
                                     <div class="hint">Requests stopped by bans or rate limits.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Cache Hits / 24h</div>
                                     <div id="metric-cache-hits" class="value">-</div>
                                     <div class="hint">Requests served from HTTP response cache.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Avg Payload</div>
                                     <div id="metric-avg-payload" class="value">-</div>
                                     <div class="hint">Average response size for tracked API calls.</div>
                                 </article>
                                 <article class="metric-card">
                                     <div class="label">Peak Hour</div>
                                     <div id="metric-peak-hour" class="value">-</div>
                                     <div class="hint">Busiest observed UTC hour in the current window.</div>
                                 </article>
                             </section>

                             <nav class="tabs" aria-label="Admin views">
                                 <button type="button" class="tab-button active" data-tab-target="overview">Overview</button>
                                 <button type="button" class="tab-button" data-tab-target="scrapers">Scrapers</button>
                                 <button type="button" class="tab-button" data-tab-target="metrics">Metrics</button>
                                 <button type="button" class="tab-button" data-tab-target="traffic">Traffic</button>
                                 <button type="button" class="tab-button" data-tab-target="bans">Bans</button>
                             </nav>

                             <section id="tab-overview" class="tab-panel active stack">
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>Top Endpoints</h2>
                                         <p class="subtle">Most requested routes across the last 24 hours.</p>
                                         <div id="top-endpoints-table"><div class="empty-state">No public endpoint activity recorded yet.</div></div>
                                     </article>
                                     <article class="panel">
                                         <h2>Top IPs</h2>
                                         <p class="subtle">Click an IP to inspect its recent request history and routes.</p>
                                         <div id="top-ips-table"><div class="empty-state">No public client activity recorded yet.</div></div>
                                     </article>
                                 </div>
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>Redis Status</h2>
                                         <p class="subtle">Readiness and cache mode for the distributed cache layer.</p>
                                         <div id="redis-status-panel"><div class="empty-state">Redis status has not been loaded yet.</div></div>
                                     </article>
                                     <article class="panel">
                                         <h2>Status Codes</h2>
                                         <p class="subtle">Most frequent response codes in the current analytics window.</p>
                                         <div id="status-code-table"><div class="empty-state">No status code statistics recorded yet.</div></div>
                                     </article>
                                 </div>
                                 <article class="panel">
                                     <h2>Daily API Activity</h2>
                                     <p class="subtle">Stored day buckets for requests, errors, blocks and cache behavior.</p>
                                     <div id="api-daily-table"><div class="empty-state">No daily API aggregates recorded yet.</div></div>
                                 </article>
                                 <article class="panel">
                                     <h2>Scraper Snapshot</h2>
                                     <p class="subtle">Current runtime state and most recent execution history.</p>
                                     <div id="overview-scraper-summary" class="grid two-up"><div class="empty-state">No scraper activity recorded yet.</div></div>
                                 </article>
                             </section>

                             <section id="tab-scrapers" class="tab-panel stack">
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Scraper Control</h2>
                                             <p class="subtle">Start all scrapers or target a specific category-backed scraper.</p>
                                         </div>
                                         <button id="refresh-scrapers-button" type="button" class="ghost-button">Refresh</button>
                                     </div>
                                     <div class="form-grid">
                                         <div class="field span-2">
                                             <label for="scraper-select">Scraper Selection</label>
                                             <select id="scraper-select">
                                                 <option value="">All registered scrapers</option>
                                             </select>
                                         </div>
                                         <div class="field">
                                             <label for="scraper-force">Execution Mode</label>
                                             <div class="checkbox-row">
                                                 <input id="scraper-force" type="checkbox">
                                                 <span>Force start request</span>
                                             </div>
                                         </div>
                                         <div class="field">
                                             <label>Actions</label>
                                             <div class="inline-actions">
                                                 <button id="start-scraper-button" type="button" class="solid-button">Start</button>
                                                 <button id="stop-scraper-button" type="button" class="ghost-button">Stop Active</button>
                                             </div>
                                         </div>
                                     </div>
                                 </article>
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Scheduled Run</h2>
                                             <p class="subtle">Change the automatic daily scraper time without restarting the API.</p>
                                         </div>
                                     </div>
                                     <div class="form-grid">
                                         <div class="field span-2">
                                             <label for="scheduled-scraper-enabled">Daily Schedule</label>
                                             <div class="checkbox-row">
                                                 <input id="scheduled-scraper-enabled" type="checkbox">
                                                 <span>Enable the automatic daily scraper run</span>
                                             </div>
                                         </div>
                                         <div class="field">
                                             <label for="scheduled-scraper-hour">Hour</label>
                                             <input id="scheduled-scraper-hour" type="number" min="0" max="23" step="1" inputmode="numeric">
                                         </div>
                                         <div class="field">
                                             <label for="scheduled-scraper-minute">Minute</label>
                                             <input id="scheduled-scraper-minute" type="number" min="0" max="59" step="1" inputmode="numeric">
                                         </div>
                                         <div class="field span-2">
                                             <label>Stored Schedule</label>
                                             <div id="scheduled-scraper-summary" class="grid two-up">
                                                 <div class="empty-state">Schedule settings have not been loaded yet.</div>
                                             </div>
                                         </div>
                                         <div class="field">
                                             <label>Action</label>
                                             <div class="inline-actions">
                                                 <button id="save-scheduled-scraper-button" type="button" class="solid-button">Save Schedule</button>
                                             </div>
                                         </div>
                                     </div>
                                 </article>
                                 <article class="panel">
                                     <h2>Runtime Status</h2>
                                     <div id="scraper-status-details" class="grid two-up"><div class="empty-state">No scraper runtime state available yet.</div></div>
                                 </article>
                                 <article class="panel">
                                     <h2>Active Scraper Tasks</h2>
                                     <div id="scraper-active-table"><div class="empty-state">No scraper tasks are running right now.</div></div>
                                 </article>
                                 <article class="panel">
                                     <h2>Recent Scraper History</h2>
                                     <div id="scraper-history-table"><div class="empty-state">No scraper history has been recorded yet.</div></div>
                                 </article>
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>Recent Changes</h2>
                                         <div id="scraper-changes-table"><div class="empty-state">No scraper changes have been recorded yet.</div></div>
                                     </article>
                                     <article class="panel">
                                         <h2>Recent Errors</h2>
                                         <div id="scraper-errors-table"><div class="empty-state">No scraper errors have been recorded yet.</div></div>
                                     </article>
                                 </div>
                             </section>

                             <section id="tab-metrics" class="tab-panel stack">
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Prometheus Metrics</h2>
                                             <p class="subtle">Inspect HTTP, runtime and raw Prometheus metrics from the running API process.</p>
                                         </div>
                                         <button id="refresh-metrics-button" type="button" class="ghost-button">Refresh</button>
                                     </div>
                                     <div id="metrics-summary" class="grid stats-grid">
                                         <div class="empty-state">Metrics have not been loaded yet.</div>
                                     </div>
                                 </article>
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Database Load</h2>
                                             <p class="subtle">Live EF command load over the rolling observation window.</p>
                                         </div>
                                     </div>
                                     <div id="database-load-summary" class="grid stats-grid">
                                         <div class="empty-state">Database load has not been observed yet.</div>
                                     </div>
                                 </article>
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>Top Commands</h2>
                                         <div id="database-load-top-commands"><div class="empty-state">Top database commands will appear here.</div></div>
                                     </article>
                                     <article class="panel">
                                         <h2>Recent Slow Commands</h2>
                                         <div id="database-load-slow-commands"><div class="empty-state">Slow database commands will appear here.</div></div>
                                     </article>
                                 </div>
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>HTTP Metrics</h2>
                                         <div id="metrics-http-table"><div class="empty-state">HTTP metrics will appear here.</div></div>
                                     </article>
                                     <article class="panel">
                                         <h2>Runtime Metrics</h2>
                                         <div id="metrics-runtime-table"><div class="empty-state">Runtime metrics will appear here.</div></div>
                                     </article>
                                 </div>
                                 <article class="panel">
                                     <h2>Other Metrics</h2>
                                     <div id="metrics-other-table"><div class="empty-state">Other metrics will appear here.</div></div>
                                 </article>
                                 <article class="panel">
                                     <h2>Raw Metrics</h2>
                                     <p class="subtle">Full Prometheus exposition text from the protected metrics endpoint.</p>
                                     <div id="metrics-raw"><div class="empty-state">Raw metrics have not been loaded yet.</div></div>
                                 </article>
                             </section>

                             <section id="tab-traffic" class="tab-panel stack">
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Request Activity</h2>
                                             <p class="subtle">Inspect who called the API, when they called it and what they hit.</p>
                                         </div>
                                         <button id="refresh-traffic-button" type="button" class="ghost-button">Refresh</button>
                                     </div>
                                     <div class="form-grid">
                                         <div class="field span-2">
                                             <label for="traffic-ip-filter">IP Filter</label>
                                             <input id="traffic-ip-filter" type="text" placeholder="Leave empty for all IPs">
                                         </div>
                                         <div class="field">
                                             <label for="traffic-days-filter">Time Window</label>
                                             <select id="traffic-days-filter">
                                                 <option value="1" selected>Last 24 hours</option>
                                                 <option value="7">Last 7 days</option>
                                                 <option value="30">Last 30 days</option>
                                             </select>
                                         </div>
                                         <div class="field">
                                             <label>Filter</label>
                                             <div class="inline-actions">
                                                 <button id="apply-traffic-filter-button" type="button" class="solid-button">Apply</button>
                                                 <button id="clear-traffic-filter-button" type="button" class="ghost-button">Clear</button>
                                             </div>
                                         </div>
                                     </div>
                                 </article>
                                 <div class="grid two-up">
                                     <article class="panel">
                                         <h2>Selected IP Summary</h2>
                                         <div id="ip-activity-summary" class="empty-state">Select an IP from the overview or apply an IP filter.</div>
                                     </article>
                                     <article class="panel">
                                         <h2>Selected IP Top Routes</h2>
                                         <div id="ip-top-endpoints"><div class="empty-state">Route statistics will appear here for the selected IP.</div></div>
                                     </article>
                                 </div>
                                 <article class="panel">
                                     <h2>Recent Request Log</h2>
                                     <div id="request-log-table"><div class="empty-state">No public request activity recorded yet.</div></div>
                                 </article>
                             </section>

                             <section id="tab-bans" class="tab-panel stack">
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Ban Management</h2>
                                             <p class="subtle">Apply or revoke IP bans directly from the dashboard.</p>
                                         </div>
                                         <button id="refresh-bans-button" type="button" class="ghost-button">Refresh</button>
                                     </div>
                                     <div class="form-grid">
                                         <div class="field">
                                             <label for="ban-ip">IP Address</label>
                                             <input id="ban-ip" type="text" placeholder="203.0.113.42">
                                         </div>
                                         <div class="field span-2">
                                             <label for="ban-reason">Reason</label>
                                             <input id="ban-reason" type="text" placeholder="Repeated abuse, scraping or attack pattern">
                                         </div>
                                         <div class="field">
                                             <label for="ban-expires-at">Expires At</label>
                                             <input id="ban-expires-at" type="datetime-local">
                                         </div>
                                         <div class="field">
                                             <label for="ban-duration-minutes">Duration (minutes)</label>
                                             <input id="ban-duration-minutes" type="number" min="1" step="1" placeholder="120">
                                         </div>
                                     </div>
                                     <p class="subtle">Set either an explicit end time or a duration. Leave both empty for a permanent ban.</p>
                                     <div class="inline-actions">
                                         <button id="ban-submit-button" type="button" class="solid-button">Ban IP</button>
                                     </div>
                                 </article>
                                 <article class="panel">
                                     <h2>Protection Rules</h2>
                                     <div id="protection-rules-summary" class="grid stats-grid">
                                         <div class="empty-state">Protection rules have not been loaded yet.</div>
                                     </div>
                                     <div id="protection-policy-table" style="margin-top:18px;"><div class="empty-state">Rate-limit policies will appear here.</div></div>
                                 </article>
                                 <article class="panel">
                                     <div class="inline-actions" style="justify-content:space-between;">
                                         <div>
                                             <h2>Rate Limit Settings</h2>
                                             <p class="subtle">Adjust persisted request protection limits without restarting the API.</p>
                                         </div>
                                         <button id="save-rate-limit-settings-button" type="button" class="solid-button">Save Rate Limits</button>
                                     </div>
                                     <div class="form-grid">
                                         <div class="field span-2">
                                             <label for="rate-limit-enabled">Global Request Protection</label>
                                             <div class="checkbox-row">
                                                 <input id="rate-limit-enabled" type="checkbox">
                                                 <span>Enable request protection and rate limiting</span>
                                             </div>
                                         </div>
                                     </div>
                                     <div id="rate-limit-settings-summary" class="grid stats-grid">
                                         <div class="empty-state">Rate-limit settings have not been loaded yet.</div>
                                     </div>
                                     <div id="rate-limit-settings-editor" style="margin-top:18px;">
                                         <div class="empty-state">Editable rate-limit settings will appear here.</div>
                                     </div>
                                 </article>
                                 <article class="panel">
                                     <h2>Active Bans</h2>
                                     <div id="ban-table"><div class="empty-state">There are currently no active bans.</div></div>
                                 </article>
                             </section>
                         </div>

                         <script>
                             const routes = {
                                 dashboard: "{{AdminAccessDefaults.AdminDashboardPath}}",
                                 login: "{{AdminAccessDefaults.LoginPath}}",
                                 scraperStatus: "/api/admin/scraper/status",
                                 scheduledScraperSettings: "/api/admin/background-jobs/scheduled-scraper",
                                 scraperHistory: "/api/admin/scraper/history?page=1&pageSize=12",
                                 scraperCatalog: "/api/admin/scrapers",
                                 scraperChanges: "/api/admin/scraper/changes?page=1&pageSize=12",
                                 scraperErrors: "/api/admin/scraper/errors?page=1&pageSize=12",
                                 scraperRun: "/api/admin/scraper/run",
                                 scraperStop: "/api/admin/scraper/stop",
                                 apiStats: "/api/admin/stats/api?days=1",
                                 metricsOverview: "/api/admin/system/metrics",
                                 databaseLoad: "/api/admin/system/database-load",
                                 requestLogs: "/api/admin/stats/requests",
                                 ipActivity: "/api/admin/stats/ip-details",
                                 redisStatus: "/api/admin/system/redis",
                                 protectionRules: "/api/admin/security/protection-rules",
                                 rateLimitSettings: "/api/admin/security/rate-limit-settings",
                                 bans: "/api/admin/bans"
                             };

                             const state = {
                                 scraperStatus: null,
                                 scheduledScraperSettings: null,
                                 metricsOverview: null,
                                 topIps: [],
                                 selectedIp: "",
                                 selectedDays: 1,
                                 theme: "light"
                             };

                             const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
                                 dateStyle: "medium",
                                 timeStyle: "short"
                             });

                             document.addEventListener("DOMContentLoaded", () => {
                                 initializeTheme();
                                 wireTabs();
                                 wireActions();
                                 refreshAll();
                                 window.setInterval(refreshAll, 30000);
                             });

                             function wireTabs() {
                                 document.querySelectorAll("[data-tab-target]").forEach(button => {
                                     button.addEventListener("click", () => {
                                         const target = button.getAttribute("data-tab-target");

                                         document.querySelectorAll("[data-tab-target]").forEach(entry => entry.classList.remove("active"));
                                         document.querySelectorAll(".tab-panel").forEach(entry => entry.classList.remove("active"));

                                         button.classList.add("active");
                                         document.getElementById(`tab-${target}`)?.classList.add("active");
                                     });
                                 });
                             }

                             function wireActions() {
                                 document.getElementById("refresh-scrapers-button")?.addEventListener("click", () => loadScrapers(true));
                                 document.getElementById("refresh-metrics-button")?.addEventListener("click", () => loadMetrics(true));
                                 document.getElementById("refresh-traffic-button")?.addEventListener("click", () => loadTraffic(true));
                                 document.getElementById("refresh-bans-button")?.addEventListener("click", () => loadBans(true));
                                 document.getElementById("apply-traffic-filter-button")?.addEventListener("click", () => loadTraffic(true));
                                 document.getElementById("clear-traffic-filter-button")?.addEventListener("click", clearTrafficFilter);
                                 document.getElementById("start-scraper-button")?.addEventListener("click", startScraper);
                                 document.getElementById("stop-scraper-button")?.addEventListener("click", stopScraper);
                                 document.getElementById("save-scheduled-scraper-button")?.addEventListener("click", saveScheduledScraperSettings);
                                 document.getElementById("ban-submit-button")?.addEventListener("click", banIp);
                                 document.getElementById("save-rate-limit-settings-button")?.addEventListener("click", saveRateLimitSettings);
                                 document.getElementById("theme-toggle-button")?.addEventListener("click", toggleTheme);
                             }

                             async function refreshAll() {
                                 await Promise.all([
                                     loadOverview(),
                                     loadScrapers(false),
                                     loadMetrics(false),
                                     loadTraffic(false),
                                     loadBans(false)
                                 ]);
                             }

                             async function loadMetrics(manualRefresh) {
                                 try {
                                     const [metrics, databaseLoad] = await Promise.all([
                                         fetchJson(routes.metricsOverview),
                                         fetchJson(routes.databaseLoad)
                                     ]);
                                     state.metricsOverview = metrics;
                                     renderMetrics(metrics);
                                     renderDatabaseLoad(databaseLoad);

                                     if (manualRefresh) {
                                         setMessage("success", "Metrics refreshed.");
                                     }
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function loadOverview() {
                                 try {
                                     const [stats, redisStatus] = await Promise.all([
                                         fetchJson(routes.apiStats),
                                         fetchJson(routes.redisStatus)
                                     ]);

                                     document.getElementById("metric-total-requests").textContent = formatInteger(stats.totalRequests);
                                     document.getElementById("metric-unique-ips").textContent = formatInteger(stats.uniqueIpCount);
                                     document.getElementById("metric-errors").textContent = formatInteger(stats.errorCount);
                                     document.getElementById("metric-avg-response").textContent = `${Math.round(stats.averageResponseTimeMs)} ms`;
                                     document.getElementById("metric-blocked").textContent = formatInteger(stats.blockedCount);
                                     document.getElementById("metric-cache-hits").textContent = formatInteger(stats.cacheHitCount);
                                     document.getElementById("metric-avg-payload").textContent = formatBytes(stats.averageResponseSizeBytes);
                                     document.getElementById("metric-peak-hour").textContent = formatHour(stats.peakRequestHourUtc);

                                     state.topIps = Array.isArray(stats.topIps) ? stats.topIps : [];

                                     renderTopEndpoints(stats.topEndpoints ?? []);
                                     renderTopIps(stats.topIps ?? []);
                                     renderStatusCodes(stats.topStatusCodes ?? []);
                                     renderDailyApiActivity(stats.daily ?? []);
                                     renderRedisStatus(redisStatus);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function loadScrapers(manualRefresh) {
                                 try {
                                     const [status, schedule, catalog, history, changes, errors] = await Promise.all([
                                         fetchJson(routes.scraperStatus),
                                         fetchJson(routes.scheduledScraperSettings),
                                         fetchJson(routes.scraperCatalog),
                                         fetchJson(routes.scraperHistory),
                                         fetchJson(routes.scraperChanges),
                                         fetchJson(routes.scraperErrors)
                                     ]);

                                     state.scraperStatus = status;
                                     state.scheduledScraperSettings = schedule;

                                     renderHeroStatus(status);
                                     renderOverviewScraperSummary(status, history.items ?? []);
                                     renderScheduledScraperSettings(schedule);
                                     renderScraperStatusDetails(status);
                                     renderActiveScrapers(status?.activeScrapers ?? []);
                                     renderScraperCatalog(catalog.items ?? []);
                                     renderScraperHistory(history.items ?? []);
                                     renderScraperChanges(changes.items ?? []);
                                     renderScraperErrors(errors.items ?? []);

                                     if (manualRefresh) {
                                         setMessage("success", "Scraper status refreshed.");
                                     }
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function saveScheduledScraperSettings() {
                                 const enabled = document.getElementById("scheduled-scraper-enabled")?.checked === true;
                                 const hour = Number.parseInt(document.getElementById("scheduled-scraper-hour")?.value ?? "", 10);
                                 const minute = Number.parseInt(document.getElementById("scheduled-scraper-minute")?.value ?? "", 10);

                                 if (!Number.isInteger(hour) || hour < 0 || hour > 23) {
                                     setMessage("info", "Schedule hour must be between 0 and 23.");
                                     return;
                                 }

                                 if (!Number.isInteger(minute) || minute < 0 || minute > 59) {
                                     setMessage("info", "Schedule minute must be between 0 and 59.");
                                     return;
                                 }

                                 try {
                                     const schedule = await fetchJson(routes.scheduledScraperSettings, {
                                         method: "PUT",
                                         body: JSON.stringify({
                                             enabled,
                                             scheduleHour: hour,
                                             scheduleMinute: minute
                                         })
                                     });

                                     state.scheduledScraperSettings = schedule;
                                     renderScheduledScraperSettings(schedule);
                                     setMessage("success", "Scheduled scraper settings saved.");
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function loadTraffic(manualRefresh) {
                                 const ipAddress = document.getElementById("traffic-ip-filter")?.value?.trim() ?? "";
                                 const days = Number.parseInt(document.getElementById("traffic-days-filter")?.value ?? "1", 10) || 1;
                                 state.selectedDays = days;

                                 try {
                                     const query = new URLSearchParams({
                                         days: String(days),
                                         page: "1",
                                         pageSize: "80"
                                     });

                                     if (ipAddress) {
                                         query.set("ipAddress", ipAddress);
                                     }

                                     const requestLogPage = await fetchJson(`${routes.requestLogs}?${query.toString()}`);
                                     renderRequestLog(requestLogPage.items ?? []);

                                     if (ipAddress) {
                                         state.selectedIp = ipAddress;
                                         await loadIpActivity(ipAddress, days);
                                     }
                                     else {
                                         renderIpActivityEmptyState();
                                     }

                                     if (manualRefresh) {
                                         setMessage("success", "Traffic view refreshed.");
                                     }
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function loadIpActivity(ipAddress, days) {
                                 try {
                                     const query = new URLSearchParams({
                                         ipAddress,
                                         days: String(days)
                                     });

                                     const details = await fetchJson(`${routes.ipActivity}?${query.toString()}`);
                                     renderIpActivity(details);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function loadBans(manualRefresh) {
                                 try {
                                     const [result, protectionRules, rateLimitSettings] = await Promise.all([
                                         fetchJson(`${routes.bans}?page=1&pageSize=100`),
                                         fetchJson(routes.protectionRules),
                                         fetchJson(routes.rateLimitSettings)
                                     ]);
                                     renderBans(result.items ?? []);
                                     renderProtectionRules(protectionRules);
                                     renderRateLimitSettings(rateLimitSettings);

                                     if (manualRefresh) {
                                         setMessage("success", "Ban list refreshed.");
                                     }
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function startScraper() {
                                 const selectedValue = document.getElementById("scraper-select")?.value ?? "";
                                 const force = document.getElementById("scraper-force")?.checked === true;

                                 const payload = {
                                     force,
                                     categorySlug: selectedValue || null,
                                     triggeredBy: "AdminDashboard"
                                 };

                                 try {
                                     const result = await fetchJson(routes.scraperRun, {
                                         method: "POST",
                                         body: JSON.stringify(payload)
                                     });

                                     setMessage("success", result.message ?? "Scraper start request sent.");
                                     await loadScrapers(false);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function stopScraper() {
                                 if (!state.scraperStatus?.isRunning) {
                                     setMessage("info", "No scraper run is currently active.");
                                     return;
                                 }

                                 const payload = {
                                     reason: "Stopped from admin dashboard.",
                                     requestedBy: "AdminDashboard"
                                 };

                                 try {
                                     const result = await fetchJson(routes.scraperStop, {
                                         method: "POST",
                                         body: JSON.stringify(payload)
                                     });

                                     setMessage("success", result.message ?? "Stop requested.");
                                     await loadScrapers(false);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function banIp() {
                                 const ipAddress = document.getElementById("ban-ip")?.value?.trim() ?? "";
                                 const reason = document.getElementById("ban-reason")?.value?.trim() ?? "";
                                 const expiresAtRaw = document.getElementById("ban-expires-at")?.value ?? "";
                                 const durationMinutesRaw = document.getElementById("ban-duration-minutes")?.value?.trim() ?? "";

                                 if (!ipAddress || !reason) {
                                     setMessage("info", "IP address and reason are required.");
                                     return;
                                 }

                                 if (expiresAtRaw && durationMinutesRaw) {
                                     setMessage("info", "Use either Expires At or Duration, not both.");
                                     return;
                                 }

                                 const durationMinutes = durationMinutesRaw ? Number.parseInt(durationMinutesRaw, 10) : null;

                                 if (durationMinutesRaw && (!Number.isInteger(durationMinutes) || durationMinutes <= 0)) {
                                     setMessage("info", "Duration must be a positive number of minutes.");
                                     return;
                                 }

                                 const payload = {
                                     ipAddress,
                                     reason,
                                     expiresAt: expiresAtRaw ? new Date(expiresAtRaw).toISOString() : null,
                                     durationMinutes,
                                     createdBy: "AdminDashboard"
                                 };

                                 try {
                                     const result = await fetchJson(routes.bans, {
                                         method: "POST",
                                         body: JSON.stringify(payload)
                                     });

                                     setMessage("success", result.message ?? "The IP has been banned.");
                                     document.getElementById("ban-ip").value = "";
                                     document.getElementById("ban-reason").value = "";
                                     document.getElementById("ban-expires-at").value = "";
                                     document.getElementById("ban-duration-minutes").value = "";
                                     await loadBans(false);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function saveRateLimitSettings() {
                                 const enabled = document.getElementById("rate-limit-enabled")?.checked === true;
                                 const rows = Array.from(document.querySelectorAll("[data-rate-limit-scope]"));

                                 const policies = rows.map(row => ({
                                     scopeKey: row.getAttribute("data-rate-limit-scope"),
                                     tokenLimit: Number.parseInt(row.querySelector("[data-field='tokenLimit']")?.value ?? "", 10),
                                     tokensPerPeriod: Number.parseInt(row.querySelector("[data-field='tokensPerPeriod']")?.value ?? "", 10),
                                     replenishmentSeconds: Number.parseInt(row.querySelector("[data-field='replenishmentSeconds']")?.value ?? "", 10),
                                     tokenQueueLimit: Number.parseInt(row.querySelector("[data-field='tokenQueueLimit']")?.value ?? "", 10),
                                     concurrentPermitLimit: Number.parseInt(row.querySelector("[data-field='concurrentPermitLimit']")?.value ?? "", 10),
                                     concurrentQueueLimit: Number.parseInt(row.querySelector("[data-field='concurrentQueueLimit']")?.value ?? "", 10)
                                 }));

                                 const invalidPolicy = policies.find(policy =>
                                     !policy.scopeKey ||
                                     !Number.isInteger(policy.tokenLimit) || policy.tokenLimit < 1 ||
                                     !Number.isInteger(policy.tokensPerPeriod) || policy.tokensPerPeriod < 1 ||
                                     !Number.isInteger(policy.replenishmentSeconds) || policy.replenishmentSeconds < 1 ||
                                     !Number.isInteger(policy.tokenQueueLimit) || policy.tokenQueueLimit < 0 ||
                                     !Number.isInteger(policy.concurrentPermitLimit) || policy.concurrentPermitLimit < 1 ||
                                     !Number.isInteger(policy.concurrentQueueLimit) || policy.concurrentQueueLimit < 0);

                                 if (invalidPolicy) {
                                     setMessage("info", "All rate-limit values must be valid non-negative integers, with token and concurrency limits starting at 1.");
                                     return;
                                 }

                                 try {
                                     const settings = await fetchJson(routes.rateLimitSettings, {
                                         method: "PUT",
                                         body: JSON.stringify({
                                             enabled,
                                             policies
                                         })
                                     });

                                     renderRateLimitSettings(settings);
                                     const protectionRules = await fetchJson(routes.protectionRules);
                                     renderProtectionRules(protectionRules);
                                     setMessage("success", "Rate-limit settings saved.");
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             async function unbanIp(ipAddress) {
                                 try {
                                     const result = await fetchJson(routes.bans, {
                                         method: "DELETE",
                                         body: JSON.stringify({
                                             ipAddress,
                                             reason: "Unbanned from admin dashboard.",
                                             requestedBy: "AdminDashboard"
                                         })
                                     });

                                     setMessage("success", result.message ?? "The IP has been unbanned.");
                                     await loadBans(false);
                                 }
                                 catch (error) {
                                     setMessage("error", error.message);
                                 }
                             }

                             function clearTrafficFilter() {
                                 document.getElementById("traffic-ip-filter").value = "";
                                 document.getElementById("traffic-days-filter").value = "1";
                                 state.selectedIp = "";
                                 state.selectedDays = 1;
                                 loadTraffic(true);
                             }

                             async function fetchJson(url, options = {}) {
                                 const response = await fetch(url, {
                                     credentials: "same-origin",
                                     headers: {
                                         "Accept": "application/json",
                                         "Content-Type": "application/json",
                                         ...(options.headers ?? {})
                                     },
                                     ...options
                                 });

                                 if (response.status === 401 || response.status === 403) {
                                     window.location.href = `${routes.login}?returnUrl=${encodeURIComponent(routes.dashboard)}`;
                                     throw new Error("Authentication is required.");
                                 }

                                 let payload = null;
                                 const contentType = response.headers.get("content-type") ?? "";

                                 if (contentType.includes("application/json")) {
                                     payload = await response.json();
                                 }
                                 else {
                                     const text = await response.text();
                                     payload = text ? { message: text } : {};
                                 }

                                 if (!response.ok) {
                                     throw new Error(payload?.message ?? `Request failed with status ${response.status}.`);
                                 }

                                 return payload;
                             }

                             function initializeTheme() {
                                 const storedTheme = window.localStorage.getItem("tibiadataapi-admin-theme");
                                 const preferredDark = window.matchMedia?.("(prefers-color-scheme: dark)")?.matches === true;
                                 applyTheme(storedTheme === "dark" || storedTheme === "light" ? storedTheme : (preferredDark ? "dark" : "light"));
                             }

                             function toggleTheme() {
                                 applyTheme(state.theme === "dark" ? "light" : "dark");
                             }

                             function applyTheme(theme) {
                                 state.theme = theme === "dark" ? "dark" : "light";
                                 document.body.setAttribute("data-theme", state.theme);
                                 window.localStorage.setItem("tibiadataapi-admin-theme", state.theme);

                                 const button = document.getElementById("theme-toggle-button");

                                 if (button) {
                                     button.textContent = state.theme === "dark" ? "Light Theme" : "Dark Theme";
                                 }
                             }

                             function renderHeroStatus(status) {
                                 const dot = document.getElementById("hero-status-dot");
                                 const text = document.getElementById("hero-status-text");
                                 const normalizedStatus = (status?.status ?? "Pending").toLowerCase();

                                 dot.className = "status-dot";

                                 if (normalizedStatus.includes("fail")) {
                                     dot.classList.add("danger");
                                 }
                                 else if (normalizedStatus.includes("cancel") || normalizedStatus.includes("pending")) {
                                     dot.classList.add("warn");
                                 }

                                 text.textContent = `${status?.status ?? "Pending"}${status?.currentCategoryName ? ` · ${status.currentCategoryName}` : ""}`;
                             }

                             function renderRedisStatus(status) {
                                 const container = document.getElementById("redis-status-panel");
                                 const normalizedStatus = String(status?.status ?? "Unknown");

                                 container.innerHTML = `
                                     <div class="stack">
                                         <div class="inline-actions" style="justify-content:space-between;">
                                             <div>
                                                 ${renderStatusChip(normalizedStatus)}
                                             </div>
                                             <div class="subtle">Checked ${formatDateTime(status?.checkedAtUtc)}</div>
                                         </div>
                                         <div class="grid two-up">
                                             ${createSummaryCard("Instance", status?.instanceName ?? "-", status?.message ?? "No Redis status message available.")}
                                             ${createSummaryCard("Modes", buildRedisModeLabel(status), "Shows whether HybridCache and OutputCache use Redis.")}
                                         </div>
                                     </div>
                                 `;
                             }

                             function renderOverviewScraperSummary(status, historyItems) {
                                 const container = document.getElementById("overview-scraper-summary");
                                 const latest = historyItems[0];

                                 container.innerHTML = [
                                     createSummaryCard("Current Status", status?.status ?? "Pending", status?.lastMessage ?? "No active scraper run."),
                                     createSummaryCard("Progress", `${status?.completedScrapers ?? 0} / ${status?.totalScrapers ?? 0}`, `${status?.activeScraperCount ?? 0} active scraper tasks`),
                                     createSummaryCard("Last Result", status?.lastResult ?? latest?.status ?? "Pending", latest?.errorMessage ?? "No recent scraper failure recorded."),
                                     createSummaryCard("Last Finished", formatDateTime(status?.lastFinishedAt ?? latest?.finishedAt), latest?.categoryName ?? "No recent category execution.")
                                 ].join("");
                             }

                             function renderScraperStatusDetails(status) {
                                 const container = document.getElementById("scraper-status-details");
                                 const rows = [
                                     ["Status", status?.status ?? "Pending"],
                                     ["Triggered By", status?.triggeredBy ?? "-"],
                                     ["Current Scraper", status?.currentScraperName ?? "-"],
                                     ["Current Category", status?.currentCategoryName ?? "-"],
                                     ["Current Slug", status?.currentCategorySlug ?? "-"],
                                     ["Progress", `${status?.completedScrapers ?? 0} / ${status?.totalScrapers ?? 0}`],
                                     ["Active Tasks", formatInteger(status?.activeScraperCount ?? 0)],
                                     ["Started", formatDateTime(status?.lastStartedAt)],
                                     ["Finished", formatDateTime(status?.lastFinishedAt)],
                                     ["Stop Requested", status?.stopRequested ? "Yes" : "No"],
                                     ["Stop Reason", status?.stopReason ?? "-"],
                                     ["Last Result", status?.lastResult ?? "-"],
                                     ["Last Message", status?.lastMessage ?? "-"]
                                 ];

                                 container.innerHTML = rows.map(([label, value]) => `
                                     <div class="panel" style="padding:16px;">
                                         <div class="subtle" style="font-size:0.78rem;text-transform:uppercase;letter-spacing:0.08em;">${escapeHtml(label)}</div>
                                     <div style="margin-top:8px;font-weight:700;">${escapeHtml(value)}</div>
                                 </div>
                             `).join("");
                             }

                             function renderScheduledScraperSettings(schedule) {
                                 document.getElementById("scheduled-scraper-enabled").checked = schedule?.enabled === true;
                                 document.getElementById("scheduled-scraper-hour").value = String(schedule?.scheduleHour ?? 0);
                                 document.getElementById("scheduled-scraper-minute").value = String(schedule?.scheduleMinute ?? 0).padStart(2, "0");

                                 const container = document.getElementById("scheduled-scraper-summary");

                                 container.innerHTML = [
                                     createSummaryCard("Status", schedule?.enabled ? "Enabled" : "Disabled", "Automatic daily scraper execution."),
                                     createSummaryCard("Daily Run", `${String(schedule?.scheduleHour ?? 0).padStart(2, "0")}:${String(schedule?.scheduleMinute ?? 0).padStart(2, "0")}`, "Stored local server time for the scheduled run."),
                                     createSummaryCard("Timeout", `${formatInteger(schedule?.timeoutMinutes ?? 0)} min`, "Maximum runtime for the scheduled job orchestration."),
                                     createSummaryCard("Last Triggered", formatDateTime(schedule?.lastTriggeredAtUtc), "UTC timestamp of the last successful scheduled trigger.")
                                 ].join("");
                             }

                             function renderMetrics(metrics) {
                                 const summary = metrics?.summary ?? {};

                                 document.getElementById("metrics-summary").innerHTML = [
                                     createSummaryCard("Metric Families", formatInteger(metrics?.metricFamilyCount ?? 0), "Unique metric families currently exposed."),
                                     createSummaryCard("Samples", formatInteger(metrics?.sampleCount ?? 0), "Individual metric samples in the current scrape."),
                                     createSummaryCard("HTTP Requests", formatMetricNumber(summary.totalHttpRequests), "Sum of observed HTTP requests."),
                                     createSummaryCard("In Progress", formatMetricNumber(summary.httpRequestsInProgress), "Requests currently executing."),
                                     createSummaryCard("Avg Duration", formatMetricDuration(summary.averageHttpRequestDurationMs), "Average HTTP request duration."),
                                     createSummaryCard("Working Set", formatMetricMegabytes(summary.processWorkingSetMegabytes), "Current process working set."),
                                     createSummaryCard("Managed Memory", formatMetricMegabytes(summary.dotNetTotalMemoryMegabytes), "Current managed heap usage."),
                                     createSummaryCard("CPU Seconds", formatMetricNumber(summary.processCpuSecondsTotal), "Accumulated process CPU time.")
                                 ].join("");

                                 renderMetricSampleTable("metrics-http-table", metrics?.httpSamples ?? [], "No HTTP metrics are available yet.");
                                 renderMetricSampleTable("metrics-runtime-table", metrics?.runtimeSamples ?? [], "No runtime metrics are available yet.");
                                 renderMetricSampleTable("metrics-other-table", metrics?.otherSamples ?? [], "No additional metrics are available yet.");

                                 const rawContainer = document.getElementById("metrics-raw");
                                 const rawText = metrics?.rawMetricsText ?? "";

                                 rawContainer.innerHTML = rawText
                                 ? `<div class="table-scroll raw-scroll"><pre style="margin:0;white-space:pre-wrap;word-break:break-word;padding:16px;background:var(--panel-muted);">${escapeHtml(rawText)}</pre></div>`
                                 : `<div class="empty-state">Raw metrics are not available.</div>`;
                             }

                             function renderDatabaseLoad(load) {
                                 const summaryContainer = document.getElementById("database-load-summary");
                                 const topCommandsContainer = document.getElementById("database-load-top-commands");
                                 const slowCommandsContainer = document.getElementById("database-load-slow-commands");

                                 summaryContainer.innerHTML = [
                                     createSummaryCard("Observed Window", `${formatInteger(load?.windowMinutes ?? 0)} min`, "Rolling in-memory window for live EF command observation."),
                                     createSummaryCard("Commands", formatInteger(load?.totalCommands ?? 0), "Total EF commands seen in the current window."),
                                     createSummaryCard("Commands / Min", formatMetricNumber(load?.commandsPerMinute ?? 0), "Average EF commands per minute in the current window."),
                                     createSummaryCard("Average", formatMetricDuration(load?.averageDurationMs ?? 0), "Average database command duration."),
                                     createSummaryCard("Max", formatMetricDuration(load?.maxDurationMs ?? 0), "Slowest database command in the current window."),
                                     createSummaryCard("Slow", formatInteger(load?.slowCommandCount ?? 0), "Commands at or above the slow-query threshold."),
                                     createSummaryCard("Failed", formatInteger(load?.failedCommandCount ?? 0), "Database commands that completed with an EF error."),
                                     createSummaryCard("Collected", formatDateTime(load?.collectedAtUtc), "UTC timestamp of the latest load snapshot.")
                                 ].join("");

                                 const topCommands = Array.isArray(load?.topCommands) ? load.topCommands : [];
                                 const recentSlowCommands = Array.isArray(load?.recentSlowCommands) ? load.recentSlowCommands : [];

                                 if (!topCommands.length) {
                                     topCommandsContainer.innerHTML = "<div class=\"empty-state\">No database commands have been observed in the current window.</div>";
                                 }
                                 else {
                                     topCommandsContainer.innerHTML = `
                                         <div class="table-scroll metrics-scroll">
                                         <table class="metrics-table">
                                             <thead>
                                                 <tr>
                                                     <th>Command</th>
                                                     <th>Count</th>
                                                     <th>Avg / Max</th>
                                                 </tr>
                                             </thead>
                                             <tbody>
                                                 ${topCommands.map(item => `
                                                     <tr>
                                                         <td>
                                                             <strong class="metrics-name">${escapeHtml(item.commandText)}</strong>
                                                             <div class="subtle metrics-help" style="font-size:0.82rem;margin-top:4px;">Last seen ${escapeHtml(formatDateTime(item.lastSeenAtUtc))}</div>
                                                         </td>
                                                         <td>${formatInteger(item.count)}${item.failedCount ? ` <span class="subtle">(${formatInteger(item.failedCount)} failed)</span>` : ""}</td>
                                                         <td>${escapeHtml(formatMetricDuration(item.averageDurationMs))} / ${escapeHtml(formatMetricDuration(item.maxDurationMs))}</td>
                                                     </tr>
                                                 `).join("")}
                                             </tbody>
                                         </table>
                                         </div>
                                     `;
                                 }

                                 if (!recentSlowCommands.length) {
                                     slowCommandsContainer.innerHTML = "<div class=\"empty-state\">No slow database commands were observed in the current window.</div>";
                                 }
                                 else {
                                     slowCommandsContainer.innerHTML = `
                                         <div class="table-scroll metrics-scroll">
                                         <table class="metrics-table">
                                             <thead>
                                                 <tr>
                                                     <th>Command</th>
                                                     <th>Duration</th>
                                                     <th>Time</th>
                                                 </tr>
                                             </thead>
                                             <tbody>
                                                 ${recentSlowCommands.map(item => `
                                                     <tr>
                                                         <td>
                                                             <strong class="metrics-name">${escapeHtml(item.commandText)}</strong>
                                                             <div class="subtle metrics-help" style="font-size:0.82rem;margin-top:4px;">${item.failed ? "Failed command" : "Completed command"}</div>
                                                         </td>
                                                         <td>${escapeHtml(formatMetricDuration(item.durationMs))}</td>
                                                         <td>${escapeHtml(formatDateTime(item.occurredAtUtc))}</td>
                                                     </tr>
                                                 `).join("")}
                                             </tbody>
                                         </table>
                                         </div>
                                     `;
                                 }
                             }

                             function renderMetricSampleTable(containerId, samples, emptyMessage) {
                                 const container = document.getElementById(containerId);

                                 if (!Array.isArray(samples) || samples.length === 0) {
                                     container.innerHTML = `<div class="empty-state">${escapeHtml(emptyMessage)}</div>`;
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll metrics-scroll">
                                     <table class="metrics-table">
                                         <thead>
                                             <tr>
                                                 <th>Name</th>
                                                 <th>Value</th>
                                                 <th>Labels</th>
                                             </tr>
                                         </thead>
                                             <tbody>
                                                 ${samples.map(sample => `
                                                     <tr>
                                                         <td>
                                                         <strong class="metrics-name">${escapeHtml(sample.name)}</strong>
                                                         <div class="subtle metrics-help" style="font-size:0.82rem;margin-top:4px;">${escapeHtml(sample.help ?? "-")}</div>
                                                     </td>
                                                         <td>${escapeHtml(formatMetricNumber(sample.value))}</td>
                                                     <td class="metrics-labels">${escapeHtml(sample.labels ?? "-")}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function renderActiveScrapers(items) {
                                 const container = document.getElementById("scraper-active-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No scraper tasks are running right now.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Log Id</th>
                                                 <th>Scraper</th>
                                                 <th>Category</th>
                                                 <th>Slug</th>
                                                 <th>Started</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td class="mono">${escapeHtml(item.scrapeLogId ?? "-")}</td>
                                                     <td class="mono">${escapeHtml(item.scraperName)}</td>
                                                     <td>${escapeHtml(item.categoryName)}</td>
                                                     <td class="mono">${escapeHtml(item.categorySlug)}</td>
                                                     <td>${formatDateTime(item.startedAt)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderScraperCatalog(items) {
                                 const select = document.getElementById("scraper-select");
                                 const previousValue = select.value;
                                 const sortedItems = [...items].sort((left, right) => {
                                     const categoryComparison = left.categoryName.localeCompare(right.categoryName);
                                     return categoryComparison !== 0
                                         ? categoryComparison
                                         : left.scraperName.localeCompare(right.scraperName);
                                 });

                                 select.innerHTML = "<option value=\"\">All registered scrapers</option>" +
                                     sortedItems.map(item => `
                                         <option value="${escapeHtml(item.categorySlug)}">
                                             ${escapeHtml(item.categoryName)} · ${escapeHtml(item.scraperName)}
                                         </option>
                                     `).join("");

                                 if ([...select.options].some(option => option.value === previousValue)) {
                                     select.value = previousValue;
                                 }
                             }

                             function renderScraperHistory(items) {
                                 const container = document.getElementById("scraper-history-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No scraper history has been recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Status</th>
                                                 <th>Scraper</th>
                                                 <th>Category</th>
                                                 <th>Started</th>
                                                 <th>Finished</th>
                                                 <th>Processed</th>
                                                 <th>Changes</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${renderStatusChip(item.status)}</td>
                                                     <td class="mono">${escapeHtml(item.scraperName ?? "-")}</td>
                                                     <td>${escapeHtml(item.categoryName ?? "-")}</td>
                                                     <td>${formatDateTime(item.startedAt)}</td>
                                                     <td>${formatDateTime(item.finishedAt)}</td>
                                                     <td>${formatInteger(item.itemsProcessed)}</td>
                                                     <td>${formatInteger(item.itemsAdded)} / ${formatInteger(item.itemsUpdated)} / ${formatInteger(item.itemsFailed)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderTopEndpoints(items) {
                                 const container = document.getElementById("top-endpoints-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No endpoint traffic recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Route</th>
                                                 <th>Method</th>
                                                 <th>Requests</th>
                                                 <th>Avg ms</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td class="mono">${escapeHtml(item.route)}</td>
                                                     <td>${escapeHtml(item.method)}</td>
                                                     <td>${formatInteger(item.requestCount)}</td>
                                                     <td>${Math.round(item.averageResponseTimeMs)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderTopIps(items) {
                                 const container = document.getElementById("top-ips-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No client activity recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>IP</th>
                                                 <th>Requests</th>
                                                 <th>Blocked</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr data-ip="${escapeHtml(item.ipAddress)}" style="cursor:pointer;">
                                                     <td class="mono">${escapeHtml(item.ipAddress)}</td>
                                                     <td>${formatInteger(item.requestCount)}</td>
                                                     <td>${formatInteger(item.blockedCount)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;

                                 container.querySelectorAll("[data-ip]").forEach(row => {
                                     row.addEventListener("click", () => {
                                         const ipAddress = row.getAttribute("data-ip");
                                         document.getElementById("traffic-ip-filter").value = ipAddress ?? "";
                                         document.getElementById("traffic-days-filter").value = "1";
                                         document.querySelector("[data-tab-target=\"traffic\"]")?.click();
                                         loadTraffic(true);
                                     });
                                 });
                             }

                             function renderStatusCodes(items) {
                                 const container = document.getElementById("status-code-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No status code statistics recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Status</th>
                                                 <th>Requests</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${escapeHtml(String(item.statusCode))}</td>
                                                     <td>${formatInteger(item.requestCount)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderDailyApiActivity(items) {
                                 const container = document.getElementById("api-daily-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No daily API aggregates recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll">
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Day</th>
                                                 <th>Requests</th>
                                                 <th>Errors</th>
                                                 <th>Blocked</th>
                                                 <th>Cache</th>
                                                 <th>Avg ms</th>
                                                 <th>Total Bytes</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${escapeHtml(formatDate(item.day))}</td>
                                                     <td>${formatInteger(item.requestCount)}</td>
                                                     <td>${formatInteger(item.errorCount)}</td>
                                                     <td>${formatInteger(item.blockedCount)}</td>
                                                     <td>${formatInteger(item.cacheHitCount)} / ${formatInteger(item.cacheMissCount)} / ${formatInteger(item.cacheBypassCount)}</td>
                                                     <td>${Math.round(item.averageResponseTimeMs)} ms</td>
                                                     <td>${formatBytes(item.totalResponseSizeBytes)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function renderRequestLog(items) {
                                 const container = document.getElementById("request-log-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No request log entries match the current filter.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll request-log-scroll">
                                     <table class="request-log-table">
                                         <thead>
                                             <tr>
                                                 <th>Time</th>
                                                 <th>IP</th>
                                                 <th>Method</th>
                                                 <th>Route</th>
                                                 <th>Cache</th>
                                                 <th>Status</th>
                                                 <th>Duration</th>
                                                 <th>Bytes</th>
                                                 <th>User Agent</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${formatDateTime(item.occurredAt)}</td>
                                                     <td class="mono">${escapeHtml(item.ipAddress)}</td>
                                                     <td>${escapeHtml(item.method)}</td>
                                                     <td class="mono">${escapeHtml(item.route)}</td>
                                                     <td>${escapeHtml(item.cacheStatus ?? "-")}</td>
                                                     <td>${escapeHtml(String(item.statusCode))}</td>
                                                     <td>${Math.round(item.durationMs)} ms</td>
                                                     <td>${formatBytes(item.responseSizeBytes)}</td>
                                                     <td class="metrics-labels">${escapeHtml(item.userAgent ?? "-")}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function renderIpActivity(details) {
                                 const summary = document.getElementById("ip-activity-summary");
                                 const endpoints = document.getElementById("ip-top-endpoints");

                                 if (!details || !details.ipAddress) {
                                     renderIpActivityEmptyState();
                                     return;
                                 }

                                 summary.innerHTML = `
                                     <div class="stack">
                                         <div class="inline-actions" style="justify-content:space-between;">
                                             <div>
                                                 <h3 style="margin:0;">${escapeHtml(details.ipAddress)}</h3>
                                                 <p class="subtle" style="margin:6px 0 0;">Observed over the last ${escapeHtml(String(details.days))} day(s).</p>
                                             </div>
                                             <button type="button" class="ghost-button" onclick="fillBanFormFromSelectedIp('${escapeJs(details.ipAddress)}')">Prepare Ban</button>
                                         </div>
                                         <div class="grid two-up">
                                             ${createSummaryCard("Requests", formatInteger(details.totalRequests), "All tracked calls from this IP.")}
                                             ${createSummaryCard("Blocked", formatInteger(details.blockedCount), "Requests denied by bans or rate limits.")}
                                             ${createSummaryCard("Errors", formatInteger(details.errorCount), "Responses with 4xx or 5xx status.")}
                                             ${createSummaryCard("Average", `${Math.round(details.averageResponseTimeMs)} ms`, "Mean response time for this IP.")}
                                             ${createSummaryCard("Payload", formatBytes(details.averageResponseSizeBytes), "Average response size for this IP.")}
                                             ${createSummaryCard("Bytes", formatBytes(details.totalResponseSizeBytes), "Total response volume in the current window.")}
                                         </div>
                                         <div class="grid two-up">
                                             <div class="panel" style="padding:16px;">
                                                 <div class="subtle" style="font-size:0.78rem;text-transform:uppercase;letter-spacing:0.08em;">First Seen</div>
                                                 <div style="margin-top:8px;font-weight:700;">${formatDateTime(details.firstSeenAt)}</div>
                                             </div>
                                             <div class="panel" style="padding:16px;">
                                                 <div class="subtle" style="font-size:0.78rem;text-transform:uppercase;letter-spacing:0.08em;">Last Seen</div>
                                                 <div style="margin-top:8px;font-weight:700;">${formatDateTime(details.lastSeenAt)}</div>
                                             </div>
                                         </div>
                                     </div>
                                 `;

                                 if (!details.topEndpoints?.length) {
                                     endpoints.innerHTML = "<div class=\"empty-state\">No route breakdown available for this IP.</div>";
                                     return;
                                 }

                                 endpoints.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Route</th>
                                                 <th>Method</th>
                                                 <th>Requests</th>
                                                 <th>Avg ms</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${details.topEndpoints.map(item => `
                                                 <tr>
                                                     <td class="mono">${escapeHtml(item.route)}</td>
                                                     <td>${escapeHtml(item.method)}</td>
                                                     <td>${formatInteger(item.requestCount)}</td>
                                                     <td>${Math.round(item.averageResponseTimeMs)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderIpActivityEmptyState() {
                                 document.getElementById("ip-activity-summary").innerHTML = "<div class=\"empty-state\">Select an IP from the overview or enter one in the filter to inspect detailed activity.</div>";
                                 document.getElementById("ip-top-endpoints").innerHTML = "<div class=\"empty-state\">Route statistics will appear here for the selected IP.</div>";
                             }

                             function renderBans(items) {
                                 const container = document.getElementById("ban-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">There are currently no active bans.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>IP</th>
                                                 <th>Reason</th>
                                                 <th>Started</th>
                                                 <th>Ends</th>
                                                 <th>Duration</th>
                                                 <th>Created By</th>
                                                 <th>Action</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td class="mono">${escapeHtml(item.ipAddress)}</td>
                                                     <td>${escapeHtml(item.reason)}</td>
                                                     <td>${formatDateTime(item.startedAt)}</td>
                                                     <td>${formatDateTime(item.endsAt)}</td>
                                                     <td>${item.durationMinutes ? `${formatInteger(item.durationMinutes)} min` : "-"}</td>
                                                     <td>${escapeHtml(item.createdBy ?? "-")}</td>
                                                     <td class="table-actions">
                                                         <button type="button" onclick="unbanIp('${escapeJs(item.ipAddress)}')">Unban</button>
                                                     </td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                 `;
                             }

                             function renderProtectionRules(payload) {
                                 const rulesContainer = document.getElementById("protection-rules-summary");
                                 const policiesContainer = document.getElementById("protection-policy-table");
                                 const rules = Array.isArray(payload?.rules) ? payload.rules : [];
                                 const policies = Array.isArray(payload?.rateLimitPolicies) ? payload.rateLimitPolicies : [];

                                 if (!rules.length) {
                                     rulesContainer.innerHTML = "<div class=\"empty-state\">Protection rules are not available.</div>";
                                 }
                                 else {
                                     rulesContainer.innerHTML = rules.map(rule =>
                                         createSummaryCard(rule.outcome, String(rule.statusCode), `${rule.trigger} ${rule.effect}`)
                                     ).join("");
                                 }

                                 if (!policies.length) {
                                     policiesContainer.innerHTML = "<div class=\"empty-state\">No rate-limit policies are available.</div>";
                                     return;
                                 }

                                 policiesContainer.innerHTML = `
                                     <div class="table-scroll">
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Scope</th>
                                                 <th>Tokens</th>
                                                 <th>Replenish</th>
                                                 <th>Queues</th>
                                                 <th>Concurrency</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${policies.map(item => `
                                                 <tr>
                                                     <td>${escapeHtml(item.scope)}</td>
                                                     <td>${formatInteger(item.tokenLimit)} / ${formatInteger(item.tokensPerPeriod)}</td>
                                                     <td>${formatInteger(item.replenishmentSeconds)} s</td>
                                                     <td>${formatInteger(item.tokenQueueLimit)} / ${formatInteger(item.concurrentQueueLimit)}</td>
                                                     <td>${formatInteger(item.concurrentPermitLimit)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function renderRateLimitSettings(payload) {
                                 const container = document.getElementById("rate-limit-settings-editor");
                                 const summary = document.getElementById("rate-limit-settings-summary");
                                 const enabledToggle = document.getElementById("rate-limit-enabled");
                                 const policies = Array.isArray(payload?.policies) ? payload.policies : [];

                                 if (enabledToggle) {
                                     enabledToggle.checked = payload?.enabled === true;
                                 }

                                 if (summary) {
                                     summary.innerHTML = `
                                         ${createSummaryCard("Rate Limiting", payload?.enabled ? "Enabled" : "Disabled", "Global request protection switch for all configured scopes.")}
                                         ${createSummaryCard("Config Version", formatInteger(payload?.version ?? 1), "Increases whenever the persisted settings change and rolls new live limiter partitions.")}
                                         ${createSummaryCard("Updated", formatDateTime(payload?.updatedAtUtc), "Timestamp of the last persisted rate-limit configuration change.")}
                                     `;
                                 }

                                 if (!policies.length) {
                                     container.innerHTML = "<div class=\"empty-state\">Rate-limit settings are not available.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll">
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Scope</th>
                                                 <th>Token Limit</th>
                                                 <th>Tokens / Period</th>
                                                 <th>Replenish (s)</th>
                                                 <th>Token Queue</th>
                                                 <th>Concurrency</th>
                                                 <th>Concurrency Queue</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${policies.map(item => `
                                                 <tr data-rate-limit-scope="${escapeHtml(item.scopeKey)}">
                                                     <td>${escapeHtml(item.scope)}</td>
                                                     <td><input type="number" min="1" step="1" value="${escapeHtml(String(item.tokenLimit))}" data-field="tokenLimit"></td>
                                                     <td><input type="number" min="1" step="1" value="${escapeHtml(String(item.tokensPerPeriod))}" data-field="tokensPerPeriod"></td>
                                                     <td><input type="number" min="1" step="1" value="${escapeHtml(String(item.replenishmentSeconds))}" data-field="replenishmentSeconds"></td>
                                                     <td><input type="number" min="0" step="1" value="${escapeHtml(String(item.tokenQueueLimit))}" data-field="tokenQueueLimit"></td>
                                                     <td><input type="number" min="1" step="1" value="${escapeHtml(String(item.concurrentPermitLimit))}" data-field="concurrentPermitLimit"></td>
                                                     <td><input type="number" min="0" step="1" value="${escapeHtml(String(item.concurrentQueueLimit))}" data-field="concurrentQueueLimit"></td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function fillBanFormFromSelectedIp(ipAddress) {
                                 document.querySelector("[data-tab-target=\"bans\"]")?.click();
                                 document.getElementById("ban-ip").value = ipAddress;
                             }

                             function renderScraperChanges(items) {
                                 const container = document.getElementById("scraper-changes-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No scraper changes have been recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll">
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Time</th>
                                                 <th>Type</th>
                                                 <th>Item</th>
                                                 <th>Category</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${formatDateTime(item.occurredAt)}</td>
                                                     <td>${escapeHtml(item.changeType)}</td>
                                                     <td>${escapeHtml(item.itemName)}</td>
                                                     <td>${escapeHtml(item.categoryName ?? "-")}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function renderScraperErrors(items) {
                                 const container = document.getElementById("scraper-errors-table");

                                 if (!items.length) {
                                     container.innerHTML = "<div class=\"empty-state\">No scraper errors have been recorded yet.</div>";
                                     return;
                                 }

                                 container.innerHTML = `
                                     <div class="table-scroll">
                                     <table>
                                         <thead>
                                             <tr>
                                                 <th>Time</th>
                                                 <th>Type</th>
                                                 <th>Scope</th>
                                                 <th>Message</th>
                                             </tr>
                                         </thead>
                                         <tbody>
                                             ${items.map(item => `
                                                 <tr>
                                                     <td>${formatDateTime(item.occurredAt)}</td>
                                                     <td>${escapeHtml(item.errorType)}</td>
                                                     <td>${escapeHtml(item.scope)}</td>
                                                     <td>${escapeHtml(item.message)}</td>
                                                 </tr>
                                             `).join("")}
                                         </tbody>
                                     </table>
                                     </div>
                                 `;
                             }

                             function createSummaryCard(label, value, hint) {
                                 return `
                                     <div class="metric-card">
                                         <div class="label">${escapeHtml(label)}</div>
                                         <div class="value">${escapeHtml(value)}</div>
                                         <div class="hint">${escapeHtml(hint)}</div>
                                     </div>
                                 `;
                             }

                             function buildRedisModeLabel(status) {
                                 const modes = [];

                                 if (status?.useRedisForHybridCache) {
                                     modes.push("HybridCache");
                                 }

                                 if (status?.useRedisForOutputCache) {
                                     modes.push("OutputCache");
                                 }

                                 return modes.length ? modes.join(" + ") : "Redis disabled";
                             }

                             function renderStatusChip(value) {
                                 const normalized = String(value ?? "Unknown").toLowerCase().replace(/[^a-z]/g, "");
                                 return `<span class="status-chip ${normalized}">${escapeHtml(String(value ?? "Unknown"))}</span>`;
                             }

                             function formatDateTime(value) {
                                 if (!value) {
                                     return "-";
                                 }

                                 const date = new Date(value);

                                 if (Number.isNaN(date.getTime())) {
                                     return "-";
                                 }

                                 return dateTimeFormatter.format(date);
                             }

                             function formatInteger(value) {
                                 return new Intl.NumberFormat().format(Number(value ?? 0));
                             }

                             function formatDate(value) {
                                 if (!value) {
                                     return "-";
                                 }

                                 const date = new Date(value);

                                 if (Number.isNaN(date.getTime())) {
                                     return escapeHtml(String(value));
                                 }

                                 return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(date);
                             }

                             function formatHour(value) {
                                 if (!value) {
                                     return "-";
                                 }

                                 const date = new Date(value);

                                 if (Number.isNaN(date.getTime())) {
                                     return "-";
                                 }

                                 return `${dateTimeFormatter.format(date)} UTC`;
                             }

                             function formatBytes(value) {
                                 const bytes = Number(value ?? 0);

                                 if (!Number.isFinite(bytes) || bytes <= 0) {
                                     return "0 B";
                                 }

                                 const units = ["B", "KB", "MB", "GB"];
                                 let unitIndex = 0;
                                 let scaled = bytes;

                                 while (scaled >= 1024 && unitIndex < units.length - 1) {
                                     scaled /= 1024;
                                     unitIndex += 1;
                                 }

                                 return `${new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(scaled)} ${units[unitIndex]}`;
                             }

                             function formatMetricNumber(value) {
                                 if (value === null || value === undefined || Number.isNaN(Number(value))) {
                                     return "-";
                                 }

                                 return new Intl.NumberFormat(undefined, {
                                     maximumFractionDigits: 2
                                 }).format(Number(value));
                             }

                             function formatMetricDuration(value) {
                                 if (value === null || value === undefined || Number.isNaN(Number(value))) {
                                     return "-";
                                 }

                                 return `${Math.round(Number(value))} ms`;
                             }

                             function formatMetricMegabytes(value) {
                                 if (value === null || value === undefined || Number.isNaN(Number(value))) {
                                     return "-";
                                 }

                                 return `${new Intl.NumberFormat(undefined, { maximumFractionDigits: 2 }).format(Number(value))} MB`;
                             }

                             function escapeHtml(value) {
                                 return String(value ?? "")
                                     .replaceAll("&", "&amp;")
                                     .replaceAll("<", "&lt;")
                                     .replaceAll(">", "&gt;")
                                     .replaceAll("\"", "&quot;")
                                     .replaceAll("'", "&#39;");
                             }

                             function escapeJs(value) {
                                 return String(value ?? "")
                                     .replaceAll("\\", "\\\\")
                                     .replaceAll("'", "\\'");
                             }

                             function setMessage(kind, text) {
                                 const element = document.getElementById("message");

                                 if (!text) {
                                     element.className = "message";
                                     element.textContent = "";
                                     return;
                                 }

                                 element.className = `message visible ${kind}`;
                                 element.textContent = text;
                             }
                         </script>
                     </body>
                     </html>
                     """;
        }
    }
}
