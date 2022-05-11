using OpenTK.Windowing.Desktop;

namespace OSLV.Core
{
	public class MainWindow : GameWindow
	{
		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
			: base(gameWindowSettings, nativeWindowSettings)
		{
		}
	}
}