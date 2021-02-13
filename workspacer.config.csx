// Development
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Shared\bin\Debug\net5.0-windows\win10-x64\workspacer.Shared.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Bar\bin\Debug\net5.0-windows\win10-x64\workspacer.Bar.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.ActionMenu\bin\Debug\net5.0-windows\win10-x64\workspacer.ActionMenu.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.FocusIndicator\bin\Debug\net5.0-windows\win10-x64\workspacer.FocusIndicator.dll"


// Production
#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

#load "C:\Users\dalyisaac\.workspacer\FloatingLayout.csx"

using System;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;


// TODO: Bar widget for enabled/disabled status
// TODO: Handle sleep https://github.com/rickbutton/workspacer/issues/47
// TODO: Toggle ignore current window
// TODO: Rename workspace
// TODO: Add workspaces inside a monitor
// TODO: Move all in this workspace to another workspace (this may exist in WorkspaceManager.cs)
// TODO: Max string length for title
// TODO: Fix ShowKeybindDialog to display Win+Shift modifiers


private static void MoveFocusedWindowToWorkspace(IConfigContext context, IWorkspace targetWorkspace)
{
    var focusedWorkspace = context.Workspaces.FocusedWorkspace;
    var window = focusedWorkspace.LastFocusedWindow;
    if (window == null)
    {
        return;
    }

    focusedWorkspace.RemoveWindow(window);
    targetWorkspace.AddWindow(window);
}

private static void SwitchWorkspaceLayout(IConfigContext context, ILayoutEngine targetLayout)
{
    ILayoutEngine[] layouts = context.DefaultLayouts();
    IWorkspace workspace = context.Workspaces.FocusedWorkspace;

    for (int i = 0; i < layouts.Length; i++)
    {
        if (targetLayout.Name == workspace.LayoutName)
        {
            return;
        }
        workspace.NextLayoutEngine();
    }
}


private static ActionMenuItemBuilder CreateActionMenuBuilder(IConfigContext context, ActionMenuPlugin actionMenu, IMonitor[] monitors, string[] monitorNames)
{
    var menuBuilder = actionMenu.Create();


    // Layout
    menuBuilder.AddMenu("layout", () =>
    {
        var layoutMenu = actionMenu.Create();

        foreach (var layout in context.DefaultLayouts())
        {
            layoutMenu.Add(layout.Name, () => SwitchWorkspaceLayout(context, layout));
        }

        return layoutMenu;
    });


    // Switch focused monitor
    menuBuilder.AddMenu("monitor", () =>
    {
        var monitorMenu = actionMenu.Create();

        monitorMenu.Add("left", () => context.Workspaces.SwitchFocusedMonitor(2));
        monitorMenu.Add("main", () => context.Workspaces.SwitchFocusedMonitor(0));
        monitorMenu.Add("right", () => context.Workspaces.SwitchFocusedMonitor(1));

        return monitorMenu;
    });


    // Move window to workspace
    menuBuilder.AddMenu("move", () =>
    {
        var moveMenu = actionMenu.Create();

        foreach (var monitor in monitors)
        {
            if (monitor.Index >= monitorNames.Length)
            {
                throw new Exception("monitor.Index >= monitorNames.Length");
            }
            var name = monitorNames[monitor.Index];

            moveMenu.AddMenu(name, () =>
            {
                var monitorMenu = actionMenu.Create();

                foreach (var workspace in context.WorkspaceContainer.GetWorkspaces(monitor))
                {
                    monitorMenu.Add(workspace.Name, () => MoveFocusedWindowToWorkspace(context, workspace));
                }

                return monitorMenu;
            });
        }

        return moveMenu;
    });


    // Workspacer
    menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
    menuBuilder.Add("enable", () => context.Enabled = true);
    menuBuilder.Add("disable", () => context.Enabled = false);
    menuBuilder.Add("restart", () => context.Restart());
    menuBuilder.Add("quit", () => context.Quit());

    return menuBuilder;
}


private static void AssignKeybindings(IConfigContext context, ActionMenuPlugin actionMenu, ActionMenuItemBuilder menuBuilder)
{
    KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
    KeyModifiers altShift = KeyModifiers.Alt | KeyModifiers.Shift;

    IKeybindManager manager = context.Keybinds;

    manager.UnsubscribeAll();
    manager.Subscribe(winShift, Keys.Left, () => context.Workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
    manager.Subscribe(winShift, Keys.Right, () => context.Workspaces.SwitchToNextWorkspace(), "switch to next workspace");

    manager.Subscribe(KeyModifiers.Alt, Keys.Tab, () => context.Workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    manager.Subscribe(altShift, Keys.Tab, () => context.Workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");

    manager.Subscribe(winShift, Keys.H, () => context.Workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    manager.Subscribe(winShift, Keys.L, () => context.Workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

    manager.Subscribe(winShift, Keys.P, () => actionMenu.ShowMenu(menuBuilder), "show menu");

    manager.Subscribe(winShift, Keys.Escape, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");

    manager.Subscribe(winShift, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");
}


static void doConfig(IConfigContext context)
{
    var monitors = context.MonitorContainer.GetAllMonitors();


    // Context bar
    context.AddBar(
        new BarPluginConfig()
        {
            LeftWidgets = () => new IBarWidget[]
            {
                new WorkspaceWidget(), new TextWidget(": "), new TitleWidget()
            },
            RightWidgets = () => new IBarWidget[]
            {
                new TimeWidget(1000, "HH:mm:ss dd-MMM-yyyy"),
                new ActiveLayoutWidget(),
            }
        }
    );
    context.AddFocusIndicator();


    // Layouts
    context.DefaultLayouts = () => new ILayoutEngine[]
    {
        new TallLayoutEngine(),
        new VertLayoutEngine(),
        new HorzLayoutEngine(),
        new FloatingLayoutEngine(),
        new FullLayoutEngine(),
    };


    // Monitors
    string[] monitorNames = new string[] { "main", "left", "right" };
    if (monitorNames.Length != monitors.Length)
    {
        throw new Exception("monitorNames.Length != monitors.Length");
    }

    // Sticky workspaces
    var sticky = new StickyWorkspaceContainer(context, StickyWorkspaceIndexMode.Local);
    for (int i = 0; i < monitors.Length; i++)
    {
        var monitor = monitors[i];
        var name = monitorNames[i];

        sticky.CreateWorkspaces(monitor, $"{name}:1", $"{name}:2", $"{name}:3");
    }
    context.WorkspaceContainer = sticky;


    // Filters
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("Zoom.exe"));

    // Action menu
    var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
    {
        RegisterKeybind = false
    });
    var menuBuilder = CreateActionMenuBuilder(context, actionMenu, monitors, monitorNames);

    AssignKeybindings(context, actionMenu, menuBuilder);
}

return new Action<IConfigContext>(doConfig);
