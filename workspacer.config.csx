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




public class WorkspacerConfig
{
    private readonly static Logger logger = Logger.Create();

    private readonly IConfigContext _context;
    private readonly int _fontSize;
    private readonly int _barHeight;
    private readonly string _fontName;
    private readonly Color _background;
    private readonly GapPlugin _gaps;
    private readonly Dictionary<string, ILayoutEngine[]> _workspaceLayoutMap = new();
    private readonly ActionMenuPlugin _actionMenu;
    private readonly ActionMenuItemBuilder _actionMenuBuilder;

    public WorkspacerConfig(IConfigContext context)
    {
        _context = context;
        _context.CanMinimizeWindows = true;

        _fontSize = 10;
        _barHeight = 21;
        _fontName = "Cascadia Code PL";
        _background = new Color(0x28, 0x32, 0x36);

        _gaps = InitGaps();
        InitBar();
        var _defaultLayouts = InitLayouts();
        InitWorkspaces(_defaultLayouts);
        InitFilters();
        InitRoutes();
        _actionMenu = InitActionMenu();
        _actionMenuBuilder = InitActionMenuBuilder();
        AssignKeybindings();
    }

    private GapPlugin InitGaps()
    {
        var gap = _barHeight - 8;
        return _context.AddGap(
            new GapPluginConfig()
            {
                InnerGap = gap,
                OuterGap = gap / 2,
                Delta = gap / 2,
            }
        );
    }

    private void InitBar()
    {
        _context.AddBar(
            new BarPluginConfig()
            {
                FontSize = _fontSize,
                BarHeight = _barHeight,
                FontName = _fontName,
                DefaultWidgetBackground = _background,
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
        _context.AddFocusIndicator();
    }

    private Func<ILayoutEngine[]> InitLayouts()
    {
        Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
        {
            new TallLayoutEngine(),
            new VertLayoutEngine(),
            new HorzLayoutEngine(),
            new FullLayoutEngine(),
        };
        _context.DefaultLayouts = defaultLayouts;
        return defaultLayouts;
    }

    private void InitWorkspaces(Func<ILayoutEngine[]> defaultLayouts)
    {
        (string, ILayoutEngine[])[] workspaces =
        {
            ("main", defaultLayouts()),
            ("todo", new ILayoutEngine[] { new VertLayoutEngine(), new TallLayoutEngine() }),
            ("cal", defaultLayouts()),
            ("chat", defaultLayouts()),
            ("🎶", defaultLayouts()),
            ("other", defaultLayouts()),
        };

        foreach ((string name, ILayoutEngine[] layouts) in workspaces)
        {
            _workspaceLayoutMap.Add(name, layouts);
            _context.WorkspaceContainer.CreateWorkspace(name, layouts);
        }
    }

    private void InitFilters()
    {
        // _context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("Zoom.exe"));
        _context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("1Password.exe"));
        _context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("pinentry.exe"));

        // The following filter means that Edge will now open on the correct display
        _context.WindowRouter.AddFilter((window) => !window.Class.Equals("Shell_TrayWnd"));
    }

    private void InitRoutes()
    {
        _context.WindowRouter.RouteProcessName("Slack", "chat");
        _context.WindowRouter.RouteProcessName("Discord", "chat");
        _context.WindowRouter.RouteProcessName("Spotify", "🎶");
        _context.WindowRouter.RouteProcessName("OUTLOOK", "cal");
        _context.WindowRouter.RouteTitle("Microsoft To Do", "todo");
    }

    private ActionMenuPlugin InitActionMenu()
    {
        return _context.AddActionMenu(new ActionMenuPluginConfig()
        {
            RegisterKeybind = false,
            MenuHeight = _barHeight,
            FontSize = _fontSize,
            FontName = _fontName,
            Background = _background,
        });
    }

    private ActionMenuItemBuilder InitActionMenuBuilder()
    {
        var menuBuilder = _actionMenu.Create();


        // Switch to workspace
        menuBuilder.AddMenu("switch", () =>
        {
            var workspaceMenu = _actionMenu.Create();
            var monitor = _context.MonitorContainer.FocusedMonitor;
            var workspaces = _context.WorkspaceContainer.GetWorkspaces(monitor);

            Func<int, Action> createChildMenu = (workspaceIndex) => () =>
            {
                _context.Workspaces.SwitchMonitorToWorkspace(monitor.Index, workspaceIndex);
            };

            int workspaceIndex = 0;
            foreach (var workspace in workspaces)
            {
                workspaceMenu.Add(workspace.Name, createChildMenu(workspaceIndex));
                workspaceIndex++;
            }

            return workspaceMenu;
        });


        // Switch layout
        menuBuilder.AddMenu("layout", () =>
        {
            var layoutMenu = _actionMenu.Create();
            var focusedWorkspace = _context.Workspaces.FocusedWorkspace;

            Func<int, Action> createChildMenu = (index) => () =>
            {
                focusedWorkspace.SwitchLayoutEngineToIndex(index);
            };

            var layouts = _workspaceLayoutMap.GetValueOrDefault(focusedWorkspace.Name, new ILayoutEngine[0]);
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
            var moveMenu = _actionMenu.Create();
            var focusedWorkspace = _context.Workspaces.FocusedWorkspace;

            var workspaces = _context.WorkspaceContainer.GetWorkspaces(focusedWorkspace).ToArray();
            Func<int, Action> createChildMenu = (index) => () => { _context.Workspaces.MoveFocusedWindowToWorkspace(index); };

            for (int i = 0; i < workspaces.Length; i++)
            {
                moveMenu.Add(workspaces[i].Name, createChildMenu(i));
            }

            return moveMenu;
        });


        // Rename workspace
        menuBuilder.AddFreeForm("rename", (name) =>
        {
            _context.Workspaces.FocusedWorkspace.Name = name;
        });


        // Create workspace
        menuBuilder.AddFreeForm("create workspace", (name) =>
        {
            _context.WorkspaceContainer.CreateWorkspace(name);
        });


        // Delete focused workspace
        menuBuilder.Add("close", () =>
        {
            _context.WorkspaceContainer.RemoveWorkspace(_context.Workspaces.FocusedWorkspace);
        });


        // Clear gaps
        menuBuilder.Add("clear gaps", () => _gaps.ClearGaps());


        // Workspacer
        menuBuilder.Add("toggle keybind helper", () => _context.Keybinds.ShowKeybindDialog());
        menuBuilder.Add("enable", () => _context.Enabled = true);
        menuBuilder.Add("disable", () => _context.Enabled = false);
        menuBuilder.Add("restart", () => _context.Restart());
        menuBuilder.Add("quit", () => _context.Quit());


        return menuBuilder;
    }

    private void AssignKeybindings()
    {
        KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;
        KeyModifiers winCtrl = KeyModifiers.Win | KeyModifiers.Control;
        KeyModifiers win = KeyModifiers.Win;

        IKeybindManager manager = _context.Keybinds;

        var workspaces = _context.Workspaces;


        manager.UnsubscribeAll();
        manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());


        manager.Subscribe(winCtrl, Keys.Left, () => workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
        manager.Subscribe(winCtrl, Keys.Right, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");

        manager.Subscribe(winShift, Keys.Left, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to previous monitor");
        manager.Subscribe(winShift, Keys.Right, () => workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to next monitor");


        manager.Subscribe(winShift, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
        manager.Subscribe(winShift, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

        manager.Subscribe(winCtrl, Keys.H, () => workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
        manager.Subscribe(winCtrl, Keys.L, () => workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");


        manager.Subscribe(winShift, Keys.K, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
        manager.Subscribe(winShift, Keys.J, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");

        manager.Subscribe(win, Keys.K, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
        manager.Subscribe(win, Keys.J, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");


        manager.Subscribe(winCtrl, Keys.Add, () => _gaps.IncrementInnerGap(), "increment inner gap");
        manager.Subscribe(winCtrl, Keys.Subtract, () => _gaps.DecrementInnerGap(), "decrement inner gap");

        manager.Subscribe(winShift, Keys.Add, () => _gaps.IncrementOuterGap(), "increment outer gap");
        manager.Subscribe(winShift, Keys.Subtract, () => _gaps.DecrementOuterGap(), "decrement outer gap");


        manager.Subscribe(winCtrl, Keys.P, () => _actionMenu.ShowMenu(_actionMenuBuilder), "show menu");

        manager.Subscribe(winShift, Keys.Escape, () => _context.Enabled = !_context.Enabled, "toggle enabled/disabled");

        manager.Subscribe(winShift, Keys.I, () => _context.ToggleConsoleWindow(), "toggle console window");
    }
}


return new Action<IConfigContext>((context) => new WorkspacerConfig(context));
