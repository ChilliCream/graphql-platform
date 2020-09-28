using System;

namespace GreenDonut
{
    /// <summary>
    /// Represents the method that will handle an
    /// <see cref="IDataLoader.RequestBuffered"/> event.
    /// </summary>
    /// <param name="sender">A <c>DataLoader</c> instance.</param>
    /// <param name="eventArgs">
    /// An object containing context related arguments.
    /// </param>
    public delegate void RequestBufferedEventHandler(IDataLoader sender, EventArgs eventArgs);
}
