using System.Net;

namespace TibiaDataApi.AdminAccess
{
    internal static class AdminLoginPageRenderer
    {
        public static string Render(string returnUrl, string antiforgeryToken, string? errorMessage = null)
        {
            string encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);
            string encodedAntiforgeryToken = WebUtility.HtmlEncode(antiforgeryToken);
            string errorHtml = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<p class=\"error\">{WebUtility.HtmlEncode(errorMessage)}</p>";

            return $$"""
                     <!DOCTYPE html>
                     <html lang="en">
                     <head>
                         <meta charset="utf-8">
                         <meta name="viewport" content="width=device-width, initial-scale=1">
                         <title>{{AdminAccessDefaults.AdminUiTitle}} Login</title>
                         <style>
                             :root {
                                 color-scheme: light;
                                 --bg: #f3efe5;
                                 --panel: #fffaf1;
                                 --line: #d5c7a5;
                                 --text: #23201a;
                                 --accent: #8b5a2b;
                                 --accent-strong: #6d431a;
                                 --error-bg: #fde8e7;
                                 --error-border: #d66a5d;
                                 --error-text: #7b2116;
                             }

                             * { box-sizing: border-box; }

                             body {
                                 margin: 0;
                                 min-height: 100vh;
                                 display: grid;
                                 place-items: center;
                                 font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
                                 background:
                                     radial-gradient(circle at top, rgba(139, 90, 43, 0.18), transparent 40%),
                                     linear-gradient(160deg, var(--bg), #efe3c6 60%, #f7f3ea 100%);
                                 color: var(--text);
                             }

                             .panel {
                                 width: min(420px, calc(100vw - 32px));
                                 padding: 28px;
                                 border: 1px solid var(--line);
                                 border-radius: 18px;
                                 background: rgba(255, 250, 241, 0.96);
                                 box-shadow: 0 24px 60px rgba(61, 43, 22, 0.18);
                             }

                             h1 {
                                 margin: 0 0 10px;
                                 font-size: 1.7rem;
                             }

                             p {
                                 margin: 0 0 18px;
                                 line-height: 1.5;
                             }

                             label {
                                 display: block;
                                 margin-bottom: 8px;
                                 font-weight: 600;
                             }

                             input[type="password"] {
                                 width: 100%;
                                 padding: 12px 14px;
                                 border: 1px solid var(--line);
                                 border-radius: 12px;
                                 font-size: 1rem;
                                 margin-bottom: 14px;
                                 background: #fff;
                             }

                             button {
                                 width: 100%;
                                 padding: 12px 14px;
                                 border: 0;
                                 border-radius: 12px;
                                 font-size: 1rem;
                                 font-weight: 700;
                                 color: #fff;
                                 background: linear-gradient(135deg, var(--accent), var(--accent-strong));
                                 cursor: pointer;
                             }

                             .hint {
                                 margin-top: 14px;
                                 font-size: 0.9rem;
                                 color: #5f584d;
                             }

                             .error {
                                 margin-bottom: 14px;
                                 padding: 12px 14px;
                                 border: 1px solid var(--error-border);
                                 border-radius: 12px;
                                 background: var(--error-bg);
                                 color: var(--error-text);
                             }
                         </style>
                     </head>
                     <body>
                         <main class="panel">
                             <h1>Admin Access</h1>
                             <p>Enter the admin password to open the protected admin dashboard.</p>
                             {{errorHtml}}
                             <form method="post" action="{{AdminAccessDefaults.LoginPath}}">
                                 <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}">
                                 <input type="hidden" name="{{AdminAccessDefaults.AntiforgeryFormFieldName}}" value="{{encodedAntiforgeryToken}}">
                                 <label for="password">Password</label>
                                 <input id="password" name="password" type="password" autocomplete="current-password" required>
                                 <button type="submit">Open Admin Panel</button>
                             </form>
                             <p class="hint">After a successful login you will be redirected to {{encodedReturnUrl}}.</p>
                         </main>
                     </body>
                     </html>
                     """;
        }
    }
}