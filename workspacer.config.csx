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

Action<IConfigContext> doConfig = (context) =>
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


    // Sticky workspaces
    var sticky = new StickyWorkspaceContainer(context, StickyWorkspaceIndexMode.Local);
    sticky.CreateWorkspaces(monitors[2], "left:1", "left:2", "left:3");
    sticky.CreateWorkspaces(monitors[0], "main:1", "main:2", "main:3");
    sticky.CreateWorkspaces(monitors[1], "right:1", "right:2", "right:3");

    context.WorkspaceContainer = sticky;


    // Action menu
    var actionMenu = context.AddActionMenu();
};
return doConfig;