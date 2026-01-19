using VoxelGame;
using VoxelGame.Core;
using VoxelGame.Engine;

var game = new Game();
var win = new VkWindow();
var graphs = new VkGraphics();

Singletons.Init(game, graphs, win);

win.Init();
win.Run();
