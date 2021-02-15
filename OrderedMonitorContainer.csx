// NativeMonitorContainer, but with the correct monitor ordering

// Development
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Native\bin\Debug\net5.0-windows\win10-x64\workspacer.Native.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Shared\bin\Debug\net5.0-windows\win10-x64\workspacer.Shared.dll"

// Production
#r "C:\Program Files\workspacer\workspacer.Native.dll"
#r "C:\Program Files\workspacer\workspacer.Shared.dll"

using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using workspacer;


public class OrderedMonitorContainer : IMonitorContainer
{
    private Monitor[] _monitors;

    public OrderedMonitorContainer()
    {
        var screens = Screen.AllScreens;
        _monitors = new Monitor[screens.Length];

        for (int i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            _monitors[i] = new Monitor(i, screen);
            if (screen.Primary)
            {
                FocusedMonitor = _monitors[i];
            }
        }
    }

    public int NumMonitors => _monitors.Length;

    public IMonitor FocusedMonitor { get; set; }

    public IMonitor[] GetAllMonitors()
    {
        return _monitors.ToArray();
    }
    public IMonitor GetMonitorAtIndex(int index)
    {
        return _monitors[index % _monitors.Length];
    }

    public IMonitor GetMonitorAtPoint(int x, int y)
    {
        var screen = Screen.FromPoint(new Point(x, y));
        return _monitors.FirstOrDefault(m => m.Screen.DeviceName == screen.DeviceName) ?? _monitors[0];
    }

    public IMonitor GetMonitorAtRect(int x, int y, int width, int height)
    {
        var screen = Screen.FromRectangle(new Rectangle(x, y, width, height));
        return _monitors.FirstOrDefault(m => m.Screen.DeviceName == screen.DeviceName) ?? _monitors[0];
    }
}