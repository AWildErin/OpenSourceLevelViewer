using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OSLV.Core;
using System;

namespace OSLV
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var nativeWindowSettings = new NativeWindowSettings()
			{
				Size = new Vector2i(1600, 900),
				Title = "Open Source Level Viewer",
				//Icon
			};

			using (var window = new MainWindow(GameWindowSettings.Default, nativeWindowSettings))
			{
				window.Run();
			}
		}
	}
}
