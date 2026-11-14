namespace Kopolart.Windows.Application.Interfaces
{
    /// <summary>
    /// Validates downloaded update packages before the launcher swaps and executes them.
    /// </summary>
    public interface IUpdatePackageVerifier
    {
        /// <summary>
        /// Returns true when the file looks like a genuine, signed Kopolart build.
        /// </summary>
        bool ValidatePackage(string filePath);
    }
}
