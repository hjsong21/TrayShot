using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;
using TrayShot.Infrastructure;

namespace TrayShot.Update;

public sealed class UpdateCoordinator
{
    private const string GitHubRepoUrl = "https://github.com/hjsongproject/trayshot";
    private static bool _isChecking = false;

    public static async Task CheckForUpdatesAsync(bool isManual = false)
    {
        if (_isChecking) return;
        _isChecking = true;

        try
        {
            Log.Update.Info($"Checking for updates on channel={AppSettings.Shared.UpdateChannel} (Manual={isManual})");

            var mgr = new UpdateManager(new GithubSource(GitHubRepoUrl, null, false));

            if (!mgr.IsInstalled)
            {
                Log.Update.Info("App is not installed via Velopack (development / portable mode). Skipping update check.");
                if (isManual)
                {
                    MessageBox.Show("설치 버전이 아니거나 개발 환경에서는 자동 업데이트를 지원하지 않습니다.", "TrayShot", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo == null)
            {
                Log.Update.Info("No new updates found. App is up to date.");
                if (isManual)
                {
                    MessageBox.Show("현재 최신 버전을 사용 중입니다.", "TrayShot", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            Log.Update.Info($"New update found: v{updateInfo.TargetFullRelease.Version}. Downloading...");

            await mgr.DownloadUpdatesAsync(updateInfo);
            Log.Update.Info("Update downloaded successfully.");

            if (isManual)
            {
                var result = MessageBox.Show(
                    $"새로운 버전(v{updateInfo.TargetFullRelease.Version})이 다운로드되었습니다.\n지금 바로 재시작하여 업데이트를 적용하시겠습니까?",
                    "TrayShot 업데이트",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    mgr.ApplyUpdatesAndRestart(updateInfo);
                }
                else
                {
                    mgr.WaitExitThenApplyUpdates(updateInfo);
                }
            }
            else
            {
                // 백그라운드 자동 확인 시: 앱 종료 시 자동 적용
                mgr.WaitExitThenApplyUpdates(updateInfo);
            }
        }
        catch (Exception ex)
        {
            Log.Update.Error($"Update check error: {ex.Message}");
            if (isManual)
            {
                MessageBox.Show($"업데이트 확인 중 오류가 발생했습니다:\n{ex.Message}", "TrayShot", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            _isChecking = false;
        }
    }
}
