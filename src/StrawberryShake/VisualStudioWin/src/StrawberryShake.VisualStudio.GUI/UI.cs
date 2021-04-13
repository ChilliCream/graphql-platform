using System;
using System.Windows;
using System.Windows.Threading;

namespace StrawberryShake.VisualStudio.GUI
{
    internal static class UI
    {
        public static void AllowUIToUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new DispatcherOperationCallback(delegate (object parameter)
                {
                    frame.Continue = false;
                    return null;
                }),
                null);

            Dispatcher.PushFrame(frame);
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}
