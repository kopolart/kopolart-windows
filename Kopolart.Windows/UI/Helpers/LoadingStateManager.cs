using System;
using System.Threading.Tasks;

namespace Kopolart.Windows.UI.Helpers
{
    /// <summary>
    /// Encapsulates loading timer states and minimum delay calculations to prevent timing races.
    /// </summary>
    public class LoadingStateManager
    {
        private const int MinInitialLoadingMs = 3500;
        private const int MinNavLoadingMs = 600;

        public bool IsUsageLogged { get; set; }
        public DateTime? FirstLoadingStartTime { get; private set; }
        public DateTime? LoadingStartTime { get; private set; }
        public string? WelcomeMessage { get; set; }

        public void StartInitialLoading()
        {
            LoadingStartTime = DateTime.Now;
            if (!IsUsageLogged)
            {
                IsUsageLogged = true;
                FirstLoadingStartTime = DateTime.Now;
            }
        }

        public void StartNavigationLoading()
        {
            LoadingStartTime = DateTime.Now;
        }

        public async Task EnforceMinimumDelayAsync()
        {
            if (FirstLoadingStartTime.HasValue)
            {
                var startTime = FirstLoadingStartTime.Value;
                FirstLoadingStartTime = null;

                var elapsed = DateTime.Now - startTime;
                var remainingMs = MinInitialLoadingMs - elapsed.TotalMilliseconds;
                if (remainingMs > 0)
                {
                    try { await Task.Delay((int)remainingMs); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LoadingStateManager] Minimum loading delay interrupted: {ex.Message}"); }
                }
            }
            else if (LoadingStartTime.HasValue)
            {
                var startTime = LoadingStartTime.Value;
                LoadingStartTime = null;

                var elapsed = DateTime.Now - startTime;
                var remainingMs = MinNavLoadingMs - elapsed.TotalMilliseconds;
                if (remainingMs > 0)
                {
                    try { await Task.Delay((int)remainingMs); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[LoadingStateManager] Minimum navigation delay interrupted: {ex.Message}"); }
                }
            }
        }
    }
}
