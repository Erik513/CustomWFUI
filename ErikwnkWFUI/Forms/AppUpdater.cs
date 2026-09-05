using System;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkCore.Updater;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// Self-contained "check for update" flow for apps hosting their releases on
    /// GitHub. Provides all the UI itself (prompt, progress, failure message) and
    /// only needs the repo coordinates and a couple of HttpClients to talk to
    /// ErikwnkCore.Updater - the consuming app never touches that library directly.
    /// </summary>
    public class AppUpdater
    {
        private readonly GitHubUpdateChecker _updateChecker;
        private readonly SelfUpdater _selfUpdater;

        /// <summary>checkHttpClient/downloadHttpClient are typically two separate <see cref="HttpClient"/>s so the download's own timeout/headers don't have to match the (usually much shorter) update-check ones.</summary>
        public AppUpdater(
            string repositoryOwner,
            string repositoryName,
            HttpClient checkHttpClient,
            HttpClient downloadHttpClient)
        {
            _updateChecker = new GitHubUpdateChecker(repositoryOwner, repositoryName, checkHttpClient);
            _selfUpdater = new SelfUpdater(downloadHttpClient);
        }

        /// <summary>
        /// Best-effort, non-blocking check against GitHub's latest release that
        /// immediately shows the "update available" prompt if there is one. Never
        /// throws; a failed or slow check just means no prompt is shown. On "Update
        /// now", downloads and swaps in the new build and exits the app so the swap
        /// helper can finish the job.
        ///
        /// For a settings screen that would rather notify about a new version once
        /// (not on every single startup) and otherwise show a quiet "update
        /// available" button instead of an unprompted popup, use the silent
        /// <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> overload together
        /// with <see cref="ShowUpdatePromptAsync"/> and
        /// <see cref="CreateUpdateAvailableButton"/> instead.
        /// </summary>
        public async Task CheckForUpdateAsync(Version currentVersion, TimeSpan checkTimeout, Form owner)
        {
            try
            {
                UpdateCheckResult result = await CheckForUpdateAsync(currentVersion, checkTimeout);

                if (result != null)
                    await ShowUpdatePromptAsync(result, currentVersion, owner);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Same GitHub check as the other overload, but silent - never shows any
        /// UI, just returns the result (or null on no update/failure/timeout) so
        /// the caller can decide when, or whether, to actually prompt. Never
        /// throws.
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, TimeSpan checkTimeout)
        {
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(checkTimeout))
                {
                    return await _updateChecker.CheckForUpdateAsync(currentVersion, timeout.Token);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Shows the "update available" prompt for a <paramref name="result"/> from
        /// the silent <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> - the
        /// same dialog+download+failure-handling flow the combined overload uses
        /// internally, just callable on its own (from a one-time startup notice, or
        /// later from an update button click). Returns how the dialog ended.
        /// </summary>
        public async Task<UpdateOutcome> ShowUpdatePromptAsync(UpdateCheckResult result, Version currentVersion, Form owner)
        {
            string displayedCurrentVersion =
                currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build;

            UpdateOutcome outcome = await UpdatePrompt.ShowUpdateAvailableAsync(
                displayedCurrentVersion,
                result.LatestVersion.ToString(),
                progress => ApplyUpdateAsync(result, progress),
                owner);

            if (outcome == UpdateOutcome.Failed)
            {
                MessageBox.Show(
                    UIStrings.Get("Update.DownloadFailedMessage"),
                    UIStrings.Get("Update.Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning,
                    owner,
                    MessageBoxSize.Small);

                OpenReleasePage(result.ReleaseUrl);
            }

            return outcome;
        }

        /// <summary>
        /// A small, ready-styled green button (with an update glyph and a
        /// tooltip) for <see cref="StyledForm.VersionStrip"/> - clicking it shows
        /// the same prompt <see cref="ShowUpdatePromptAsync"/> does. Add it once
        /// you know an update is available (e.g. after
        /// <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> returned a
        /// non-null result), typically when building the settings screen.
        /// </summary>
        public Button CreateUpdateAvailableButton(UpdateCheckResult result, Version currentVersion, Form owner)
        {
            Button button = UIButtonFactory.CreateGreen(
                "▲",
                UIStrings.Get("Update.AvailableTooltip"),
                new Size(22, 20),
                isIcon: true);

            // "▲" fills its glyph box (unlike the wispy "⬆"); a smaller size
            // keeps it from clipping in this strip-sized button.
            button.Font = new Font("Segoe UI Symbol", 9f);
            button.Padding = new Padding(0);
            // A FlowLayoutPanel top-aligns its children; center the button in
            // the strip. No right margin - it should sit in the corner.
            button.Margin = new Padding(6, 2, 0, 2);

            // Keep the tooltip in step with a live language switch, the way the
            // title bar's own buttons do.
            EventHandler relocalize = (s, e) =>
                UIButtonFactory.UpdateTooltip(button, UIStrings.Get("Update.AvailableTooltip"));
            UIStrings.LanguageChanged += relocalize;
            button.Disposed += (s, e) => UIStrings.LanguageChanged -= relocalize;

            button.Click += async (sender, e) => await ShowUpdatePromptAsync(result, currentVersion, owner);

            return button;
        }

        private async Task<bool> ApplyUpdateAsync(UpdateCheckResult result, IProgress<int> downloadProgress)
        {
            if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                return false;
            }

            bool prepared = await _selfUpdater.DownloadAndPrepareUpdateAsync(
                result.DownloadUrl,
                downloadProgress,
                CancellationToken.None);

            if (prepared)
            {
                Application.Exit();
            }

            return prepared;
        }

        private static void OpenReleasePage(string releaseUrl)
        {
            if (string.IsNullOrWhiteSpace(releaseUrl))
            {
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = releaseUrl,
                UseShellExecute = true
            });
        }
    }
}
