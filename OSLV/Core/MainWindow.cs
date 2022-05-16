using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace OSLV.Core
{
	public class MainWindow : GameWindow
	{
		private ImGuiController ImGuiController;

		public MainWindow( GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings )
			: base( gameWindowSettings, nativeWindowSettings )
		{
		}

		protected override void OnResize( ResizeEventArgs e )
		{
			base.OnResize( e );

			GL.Viewport( 0, 0, Size.X, Size.Y );
			ImGuiController.WindowResized( Size.X, Size.Y );
		}

		protected override void OnLoad()
		{
			base.OnLoad();

			GL.ClearColor( 0f, 0f, 0f, 1.0f );
			GL.Enable( EnableCap.DepthTest );

			ImGuiController = new ImGuiController( Size.X, Size.Y );
		}

		protected override void OnRenderFrame( FrameEventArgs e )
		{
			base.OnRenderFrame( e );

			ImGuiController.Update( this, (float)e.Time );

			GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );

			if ( ImGui.BeginMainMenuBar() )
			{
				if ( ImGui.BeginMenu( "File" ) )
				{
					if ( ImGui.MenuItem( "Open", "CTRL+O" ) )
					{
					}

					ImGui.Separator();
					if ( ImGui.MenuItem( "Save", "CTRL+S" ) )
					{
					}

					if ( ImGui.MenuItem( "Save As", "CTRL+Shift+S" ) )
					{
					}

					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}

			ImGuiController.Render();

			SwapBuffers();
		}

		protected override void OnUpdateFrame( FrameEventArgs args )
		{
			base.OnUpdateFrame( args );
		}

		protected override void OnTextInput( TextInputEventArgs e )
		{
			base.OnTextInput( e );
		}

		protected override void OnMouseWheel( MouseWheelEventArgs e )
		{
			base.OnMouseWheel( e );
		}
	}
}
