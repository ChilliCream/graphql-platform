using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// Represents a promise that can be canceled.
/// </summary>
public interface IPromise
{
    /// <summary>
    /// Gets the task that represents the async work for this promise.
    /// </summary>
    Task Task { get; }
    
    /// <summary>
    /// Tries to cancel the async work for this promise.
    /// </summary>
    void TryCancel();
}