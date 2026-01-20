using VoxelGame;
using VoxelGame.Core;
using VoxelGame.Engine;

Singletons.Init(
    new Game(),
    new VkGraphics(),
    new VkWindow(),
    new VkInput());

var win = (VkWindow)Singletons.Window;
win.Init();
win.Run();
