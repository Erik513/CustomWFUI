using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        /// Best-effort, non-blocking check against GitHub's latest release. Never
        /// throws; a failed or slow check just means no prompt is shown. On "Update
        /// now", downloads and swaps in the new build and exits the app so the swap
        /// helper can finish the job.
        /// </summary>
        public async Task CheckForUpdateAsync(Version currentVersion, TimeSpan checkTimeout, Form owner)
        {
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(checkTimeout))
                {
                    UpdateCheckResult result = await _updateChecker.CheckForUpdateAsync(
                        currentVersion,
                        timeout.Token);

                    if (result == null)
                    {
                        return;
                    }

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
                }
            }
            catch
            {
            }
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
