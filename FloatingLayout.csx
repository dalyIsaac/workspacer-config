// Based on https://github.com/rickbutton/workspacer/pull/177/


#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;


public class FloatingLayoutEngine : ILayoutEngine
{
    public IEnumerable<IWindowLocation> CalcLayout(IEnumerable<IWindow> windows, int spaceWidth, int spaceHeight)
    {
        var list = new List<IWindowLocation>();
        var numWindows = windows.Count();

        if (numWindows == 0)
            return list;

        var windowList = windows.ToList();

        for (var i = 0; i < numWindows; i++)
        {
            var window = windowList[i];
            list.Add(new WindowLocation(
                        0, 0,
                        window.Location.Width - window.Offset.Width,
                        window.Location.Height - window.Offset.Height,
                        window.Location.State
            ));
        }
        return list;
    }

    public string Name => "float";

    public void ShrinkPrimaryArea() { }
    public void ExpandPrimaryArea() { }
    public void ResetPrimaryArea() { }
    public void IncrementNumInPrimary() { }
    public void DecrementNumInPrimary() { }
}