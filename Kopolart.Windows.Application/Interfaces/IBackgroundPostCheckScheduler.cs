namespace Kopolart.Windows.Application.Interfaces
{
    /// <summary>
    /// Manages the Windows Task Scheduler entry that runs a lightweight new-post check while the
    /// application itself is closed (daily background toast).
    /// </summary>
    public interface IBackgroundPostCheckScheduler
    {
        /// <summary>True when the scheduled task currently exists for this user.</summary>
        bool IsRegistered();

        /// <summary>
        /// Creates the scheduled task when notifications are enabled and removes it when they are
        /// disabled. Runs in the background and never throws (failures are only logged).
        /// </summary>
        void Sync(bool notificationsEnabled);
    }
}
