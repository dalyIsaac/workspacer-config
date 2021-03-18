// Development
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Shared\bin\Debug\net5.0-windows\win10-x64\workspacer.Shared.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Bar\bin\Debug\net5.0-windows\win10-x64\workspacer.Bar.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.Gap\bin\Debug\net5.0-windows\win10-x64\workspacer.Gap.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.ActionMenu\bin\Debug\net5.0-windows\win10-x64\workspacer.ActionMenu.dll"
// #r "C:\Users\dalyisaac\Repos\workspacer\src\workspacer.FocusIndicator\bin\Debug\net5.0-windows\win10-x64\workspacer.FocusIndicator.dll"


// Production
#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"


using System;
using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.Gap;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;


private static Logger logger = Logger.Create();



private static ActionMenuItemBuilder CreateActionMenuBuilder(IConfigContext context, ActionMenuPlugin actionMenu, GapPlugin gaps, Dictionary<string, ILayoutEngine[]> workspaceLayoutMap)
{
    var menuBuilder = actionMenu.Create();


    // Switch layout
    menuBuilder.AddMenu("switch", () =>
    {
        var layoutMenu = actionMenu.Create();
        var focusedWorkspace = context.Workspaces.FocusedWorkspace;

        Func<int, Action> createChildMenu = (index) => () =>
        {
            focusedWorkspace.SwitchLayoutEngineToIndex(index);
        };

        var layouts = workspaceLayoutMap.GetValueOrDefault(focusedWorkspace.Name, new ILayoutEngine[0]);
        for (int index = 0; index < layouts.Length; index++)
        {
            var currentLayout = layouts[index];
            layoutMenu.Add(currentLayout.Name, createChildMenu(index));
        }

        return layoutMenu;
    });


    // Move window to workspace
    menuBuilder.AddMenu("move", () =>
    {
        var moveMenu = actionMenu.Create();
        var focusedWorkspace = context.Workspaces.FocusedWorkspace;

        var workspaces = context.WorkspaceContainer.GetWorkspaces(focusedWorkspace).ToArray();
        Func<int, Action> createChildMenu = (index) => () => { context.Workspaces.MoveFocusedWindowToWorkspace(index); };

        for (int i = 0; i < workspaces.Length; i++)
        {
            moveMenu.Add(workspaces[i].Name, createChildMenu(i));
        }

        return moveMenu;
    });


    // Rename workspace
    menuBuilder.AddFreeForm("rename", (name) =>
    {
        context.Workspaces.FocusedWorkspace.Name = name;
    });


    // Create workspace
    menuBuilder.AddFreeForm("create workspace", (name) =>
    {
        context.WorkspaceContainer.CreateWorkspace(name);
    });


    // Delete focused workspace
    menuBuilder.Add("close", () =>
    {
        context.WorkspaceContainer.RemoveWorkspace(context.Workspaces.FocusedWorkspace);
    });


    // Clear gaps
    menuBuilder.Add("clear gaps", () => gaps.ClearGaps());


    // Workspacer
    menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
    menuBuilder.Add("enable", () => context.Enabled = true);
    menuBuilder.Add("disable", () => context.Enabled = false);
    menuBuilder.Add("restart", () => context.Restart());
    menuBuilder.Add("quit", () => context.Quit());

    return menuBuilder;
}


private static void AssignKeybindings(IConfigContext context, ActionMenuPlugin actionMenu, ActionMenuItemBuilder menuBuilder, GapPlugin gaps)
{
    KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
    KeyModifiers winCtrl = KeyModifiers.Win | KeyModifiers.Control;
    KeyModifiers win = KeyModifiers.Win;

    IKeybindManager manager = context.Keybinds;

    manager.UnsubscribeAll();
    manager.Subscribe(MouseEvent.LButtonDown, () => context.Workspaces.SwitchFocusedMonitorToMouseLocation());

    manager.Subscribe(winCtrl, Keys.Left, () => context.Workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
    manager.Subscribe(winCtrl, Keys.Right, () => context.Workspaces.SwitchToNextWorkspace(), "switch to next workspace");

    manager.Subscribe(winShift, Keys.Left, () => context.Workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to previous monitor");
    manager.Subscribe(winShift, Keys.Right, () => context.Workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to next monitor");


    manager.Subscribe(winShift, Keys.H, () => context.Workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    manager.Subscribe(winShift, Keys.L, () => context.Workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

    manager.Subscribe(winCtrl, Keys.H, () => context.Workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
    manager.Subscribe(winCtrl, Keys.L, () => context.Workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");


    manager.Subscribe(winShift, Keys.K, () => context.Workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
    manager.Subscribe(winShift, Keys.J, () => context.Workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");

    manager.Subscribe(win, Keys.K, () => context.Workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    manager.Subscribe(win, Keys.J, () => context.Workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");


    manager.Subscribe(winCtrl, Keys.Add, () => gaps.IncrementInnerGap(), "increment inner gap");
    manager.Subscribe(winCtrl, Keys.Subtract, () => gaps.DecrementInnerGap(), "decrement inner gap");

    manager.Subscribe(winShift, Keys.Add, () => gaps.IncrementOuterGap(), "increment outer gap");
    manager.Subscribe(winShift, Keys.Subtract, () => gaps.DecrementOuterGap(), "decrement outer gap");


    manager.Subscribe(winShift, Keys.P, () => actionMenu.ShowMenu(menuBuilder), "show menu");

    manager.Subscribe(winShift, Keys.Escape, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");

    manager.Subscribe(winShift, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");
}


static void doConfig(IConfigContext context)
{
    var monitors = context.MonitorContainer.GetAllMonitors();
    var workspaceContainer = context.WorkspaceContainer;
    var fontSize = 12;
    var barHeight = 22;
    var fontName = "Cascadia Code PL";
    var background = new Color(20, 20, 20);

    // Gaps
    var gaps = context.AddGap(
       new GapPluginConfig()
       {
           InnerGap = barHeight,
           OuterGap = barHeight / 2,
           Delta = barHeight / 2,
       }
    );


    // Context bar
    context.AddBar(
        new BarPluginConfig()
        {
            FontSize = fontSize,
            BarHeight = barHeight,
            FontName = fontName,
            DefaultWidgetBackground = background,
            LeftWidgets = () => new IBarWidget[]
            {
                new WorkspaceWidget(), new TextWidget(": "), new TitleWidget()
            },
            RightWidgets = () => new IBarWidget[]
            {
                new BatteryWidget(),
                new TimeWidget(1000, "HH:mm:ss dd-MMM-yyyy"),
                new ActiveLayoutWidget(),
            }
        }
    );
    context.AddFocusIndicator();


    // Layouts
    var defaultLayouts = new ILayoutEngine[]
    {
        new TallLayoutEngine(),
        new VertLayoutEngine(),
        new HorzLayoutEngine(),
        new FullLayoutEngine(),
    };
    context.DefaultLayouts = () => defaultLayouts;


    // Workspaces
    (string, ILayoutEngine[])[] workspaces =
    {
        ("main", defaultLayouts),
        ("cal", defaultLayouts),
        ("todo", new ILayoutEngine[] { new VertLayoutEngine(), new TallLayoutEngine() }),
        ("chat", defaultLayouts),
        ("🎶", defaultLayouts),
        ("other", defaultLayouts),
    };

    var workspaceLayoutMap = new Dictionary<string, ILayoutEngine[]>();
    foreach ((string name, ILayoutEngine[] layouts) in workspaces)
    {
        workspaceLayoutMap.Add(name, defaultLayouts);
        workspaceContainer.CreateWorkspace(name, layouts);
    }


    // Filters
    // context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("Zoom.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("1Password.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("pinentry.exe"));

    // The following filter means that Edge will now open on the correct display
    context.WindowRouter.AddFilter((window) => !window.Class.Equals("Shell_TrayWnd"));


    // Routes
    context.WindowRouter.RouteProcessName("Slack", "chat");
    context.WindowRouter.RouteProcessName("Discord", "chat");
    context.WindowRouter.RouteProcessName("Spotify", "🎶");
    context.WindowRouter.RouteProcessName("OUTLOOK", "cal");
    context.WindowRouter.RouteTitle("Microsoft To Do", "todo");

    // Action menu
    var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
    {
        RegisterKeybind = false,
        MenuHeight = barHeight,
        FontSize = fontSize,
        FontName = fontName,
        Background = background,
    });
    var menuBuilder = CreateActionMenuBuilder(context, actionMenu, gaps, workspaceLayoutMap);

    AssignKeybindings(context, actionMenu, menuBuilder, gaps);
}

return new Action<IConfigContext>(doConfig);
