namespace Kopolart.Windows.Application.Interfaces
{
    public interface IAutoStartService
    {
        bool IsAutoStartConfigured();
        void SetAutoStart(bool enabled);
    }
}
