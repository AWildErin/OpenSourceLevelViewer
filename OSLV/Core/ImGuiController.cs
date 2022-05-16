using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OSLV.Renderer.Shaders;
using OSLV.Renderer.Textures;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Image = SixLabors.ImageSharp.Image;

namespace OSLV.Core
{
	/// <summary>
	/// A modified version of Veldrid.ImGui's ImGuiRenderer.
	/// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
	/// </summary>
	public class ImGuiController : IDisposable
	{
		private bool FrameBegun;

		private int VertexArray;
		private int VertexBuffer;
		private int VertexBufferSize;
		private int IndexBuffer;
		private int IndexBufferSize;

		private Texture FontTexture;
		private Shader Shader;

		private int WindowWidth;
		private int WindowHeight;

		private System.Numerics.Vector2 ScaleFactor = System.Numerics.Vector2.One;

		/// <summary>
		/// Constructs a new ImGuiController.
		/// </summary>
		public ImGuiController( int width, int height )
		{
			WindowWidth = width;
			WindowHeight = height;

			IntPtr context = ImGui.CreateContext();
			ImGui.SetCurrentContext( context );

			var io = ImGui.GetIO();
			io.Fonts.AddFontDefault();

			io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

			CreateDeviceResources();
			SetKeyMappings();

			SetPerFrameImGuiData( 1f / 60f );

			ImGui.NewFrame();
			FrameBegun = true;
		}

		public void WindowResized( int width, int height )
		{
			WindowWidth = width;
			WindowHeight = height;
		}

		public void DestroyDeviceObjects()
		{
			Dispose();
		}

		public void CreateDeviceResources()
		{
			VertexArray = GL.GenVertexArray();

			VertexBufferSize = 10000;
			IndexBufferSize = 2000;

			VertexBuffer = GL.GenBuffer();
			IndexBuffer = GL.GenBuffer();

			GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBuffer );
			GL.BufferData( BufferTarget.ArrayBuffer, VertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw );
			GL.BindBuffer( BufferTarget.ArrayBuffer, IndexBuffer );
			GL.BufferData( BufferTarget.ArrayBuffer, IndexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw );
			GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );

			RecreateFontDeviceTexture();

			Shader = new Shader( "Content/Core/shaders/imgui.vsh", "Content/Core/shaders/imgui.fsh" );

			GL.BindVertexArray( VertexArray );

			GL.BindBuffer( BufferTarget.ArrayBuffer, VertexBuffer );
			GL.BindBuffer( BufferTarget.ElementArrayBuffer, IndexBuffer );

			int stride = Unsafe.SizeOf<ImDrawVert>();

			GL.EnableVertexAttribArray( 0 );
			GL.VertexAttribPointer( 0, 2, VertexAttribPointerType.Float, false, stride, 0 );
			GL.EnableVertexAttribArray( 1 );
			GL.VertexAttribPointer( 1, 2, VertexAttribPointerType.Float, false, stride, 8 );
			GL.EnableVertexAttribArray( 2 );
			GL.VertexAttribPointer( 2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16 );

			GL.BindVertexArray( 0 );
			GL.BindBuffer( BufferTarget.ArrayBuffer, 0 );
		}

		/// <summary>
		/// Recreates the device texture used to render text.
		/// </summary>
		public void RecreateFontDeviceTexture()
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.Fonts.GetTexDataAsRGBA32( out IntPtr pixels, out int width, out int height, out int bytesPerPixel );

			byte[] buffer = new byte[height * width * bytesPerPixel];
			Marshal.Copy( pixels, buffer, 0, height * width * bytesPerPixel );

			FontTexture = Texture.LoadFromImage( Image.LoadPixelData<Rgba32>( buffer, width, height ) );

			io.Fonts.SetTexID( (IntPtr)FontTexture.Handle );

			io.Fonts.ClearTexData();
		}

		/// <summary>
		/// Renders the ImGui draw list data.
		/// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
		/// or index data has increased beyond the capacity of the existing buffers.
		/// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
		/// </summary>
		public void Render()
		{
			if ( FrameBegun )
			{
				FrameBegun = false;
				ImGui.Render();
				RenderImDrawData( ImGui.GetDrawData() );
			}
		}

		/// <summary>
		/// Updates ImGui input and IO configuration state.
		/// </summary>
		public void Update( GameWindow wnd, float dt )
		{
			RenderStyle();

			if ( FrameBegun )
			{
				ImGui.Render();
			}

			SetPerFrameImGuiData( dt );
			UpdateImGuiInput( wnd );

			FrameBegun = true;
			ImGui.NewFrame();
		}

		/// <summary>
		/// Sets per-frame data based on the associated window.
		/// This is called by Update(float).
		/// </summary>
		private void SetPerFrameImGuiData( float dt )
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.DisplaySize = new System.Numerics.Vector2(
				WindowWidth / ScaleFactor.X,
				WindowHeight / ScaleFactor.Y );
			io.DisplayFramebufferScale = ScaleFactor;
			io.DeltaTime = dt; // DeltaTime is in seconds.
		}

		readonly List<char> PressedChars = new List<char>();

		private void UpdateImGuiInput( GameWindow wnd )
		{
			ImGuiIOPtr io = ImGui.GetIO();

			MouseState MouseState = wnd.MouseState;
			KeyboardState KeyboardState = wnd.KeyboardState;

			io.MouseDown[0] = MouseState[MouseButton.Left];
			io.MouseDown[1] = MouseState[MouseButton.Right];
			io.MouseDown[2] = MouseState[MouseButton.Middle];

			var screenPoint = new Vector2i( (int)MouseState.X, (int)MouseState.Y );
			var point = screenPoint;
			io.MousePos = new System.Numerics.Vector2( point.X, point.Y );

			foreach ( Keys key in Enum.GetValues( typeof( Keys ) ) )
			{
				if ( key == Keys.Unknown )
				{
					continue;
				}
				io.KeysDown[(int)key] = KeyboardState.IsKeyDown( key );
			}

			foreach ( var c in PressedChars )
			{
				io.AddInputCharacter( c );
			}
			PressedChars.Clear();

			io.KeyCtrl = KeyboardState.IsKeyDown( Keys.LeftControl ) || KeyboardState.IsKeyDown( Keys.RightControl );
			io.KeyAlt = KeyboardState.IsKeyDown( Keys.LeftAlt ) || KeyboardState.IsKeyDown( Keys.RightAlt );
			io.KeyShift = KeyboardState.IsKeyDown( Keys.LeftShift ) || KeyboardState.IsKeyDown( Keys.RightShift );
			io.KeySuper = KeyboardState.IsKeyDown( Keys.LeftSuper ) || KeyboardState.IsKeyDown( Keys.RightSuper );
		}

		internal void PressChar( char keyChar )
		{
			PressedChars.Add( keyChar );
		}

		internal void MouseScroll( Vector2 offset )
		{
			ImGuiIOPtr io = ImGui.GetIO();

			io.MouseWheel = offset.Y;
			io.MouseWheelH = offset.X;
		}

		private static void SetKeyMappings()
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
			io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
			io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
			io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
			io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
			io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
			io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
			io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
			io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
			io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
			io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
			io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
			io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
			io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
			io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
			io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
			io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
			io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
			io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;
		}

		private void RenderImDrawData( ImDrawDataPtr drawData )
		{
			if ( drawData.CmdListsCount == 0 )
			{
				return;
			}

			for ( int i = 0; i < drawData.CmdListsCount; i++ )
			{
				ImDrawListPtr cmd_list = drawData.CmdListsRange[i];

				int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
				if ( vertexSize > VertexBufferSize )
				{
					int newSize = (int)Math.Max( VertexBufferSize * 1.5f, vertexSize );
					GL.NamedBufferData( VertexBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw );
					VertexBufferSize = newSize;
				}

				int indexSize = cmd_list.IdxBuffer.Size * sizeof( ushort );
				if ( indexSize > IndexBufferSize )
				{
					int newSize = (int)Math.Max( IndexBufferSize * 1.5f, indexSize );
					GL.NamedBufferData( IndexBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw );
					IndexBufferSize = newSize;
				}
			}

			// Setup orthographic projection matrix into our constant buffer
			ImGuiIOPtr io = ImGui.GetIO();
			Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
				0.0f,
				io.DisplaySize.X,
				io.DisplaySize.Y,
				0.0f,
				-1.0f,
				1.0f );

			Shader.Bind();
			Shader.SetMatrix4( "projection_matrix", mvp );
			Shader.SetInt( "in_fontTexture", 0 );

			FontTexture.Use( TextureUnit.Texture0 );

			GL.BindVertexArray( VertexArray );

			drawData.ScaleClipRects( io.DisplayFramebufferScale );

			GL.Enable( EnableCap.Blend );
			GL.Enable( EnableCap.ScissorTest );
			GL.BlendEquation( BlendEquationMode.FuncAdd );
			GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
			GL.Disable( EnableCap.CullFace );
			GL.Disable( EnableCap.DepthTest );

			// Render command lists
			for ( int n = 0; n < drawData.CmdListsCount; n++ )
			{
				ImDrawListPtr cmd_list = drawData.CmdListsRange[n];

				GL.NamedBufferSubData( VertexBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data );

				GL.NamedBufferSubData( IndexBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof( ushort ), cmd_list.IdxBuffer.Data );

				int vtx_offset = 0;
				int idx_offset = 0;

				for ( int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++ )
				{
					ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
					if ( pcmd.UserCallback != IntPtr.Zero )
					{
						throw new NotImplementedException();
					}
					else
					{
						GL.ActiveTexture( TextureUnit.Texture0 );
						GL.BindTexture( TextureTarget.Texture2D, (int)pcmd.TextureId );

						// We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
						var clip = pcmd.ClipRect;
						GL.Scissor( (int)clip.X, WindowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y) );

						if ( (io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0 )
						{
							GL.DrawElementsBaseVertex( PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(idx_offset * sizeof( ushort )), vtx_offset );
						}
						else
						{
							GL.DrawElements( BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof( ushort ) );
						}
					}

					idx_offset += (int)pcmd.ElemCount;
				}
				vtx_offset += cmd_list.VtxBuffer.Size;
			}

			GL.Disable( EnableCap.Blend );
			GL.Disable( EnableCap.ScissorTest );
		}

		public void RenderStyle()
		{
			var style = ImGui.GetStyle();
			var colors = style.Colors;

			// From: https://github.com/ocornut/imgui/issues/707#issuecomment-917151020

			colors[(int)ImGuiCol.Text] = new System.Numerics.Vector4( 1.00f, 1.00f, 1.00f, 1.00f );
			colors[(int)ImGuiCol.TextDisabled] = new System.Numerics.Vector4( 0.50f, 0.50f, 0.50f, 1.00f );
			colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4( 0.10f, 0.10f, 0.10f, 1.00f );
			colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.00f );
			colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4( 0.19f, 0.19f, 0.19f, 0.92f );
			colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4( 0.19f, 0.19f, 0.19f, 0.29f );
			colors[(int)ImGuiCol.BorderShadow] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.24f );
			colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4( 0.05f, 0.05f, 0.05f, 0.54f );
			colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4( 0.19f, 0.19f, 0.19f, 0.54f );
			colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4( 0.20f, 0.22f, 0.23f, 1.00f );
			colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4( 0.06f, 0.06f, 0.06f, 1.00f );
			colors[(int)ImGuiCol.TitleBgCollapsed] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4( 0.14f, 0.14f, 0.14f, 1.00f );
			colors[(int)ImGuiCol.ScrollbarBg] = new System.Numerics.Vector4( 0.05f, 0.05f, 0.05f, 0.54f );
			colors[(int)ImGuiCol.ScrollbarGrab] = new System.Numerics.Vector4( 0.34f, 0.34f, 0.34f, 0.54f );
			colors[(int)ImGuiCol.ScrollbarGrabHovered] = new System.Numerics.Vector4( 0.40f, 0.40f, 0.40f, 0.54f );
			colors[(int)ImGuiCol.ScrollbarGrabActive] = new System.Numerics.Vector4( 0.56f, 0.56f, 0.56f, 0.54f );
			colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4( 0.33f, 0.67f, 0.86f, 1.00f );
			colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4( 0.34f, 0.34f, 0.34f, 0.54f );
			colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4( 0.56f, 0.56f, 0.56f, 0.54f );
			colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4( 0.05f, 0.05f, 0.05f, 0.54f );
			colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4( 0.19f, 0.19f, 0.19f, 0.54f );
			colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4( 0.20f, 0.22f, 0.23f, 1.00f );
			colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.52f );
			colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.36f );
			colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4( 0.20f, 0.22f, 0.23f, 0.33f );
			colors[(int)ImGuiCol.Separator] = new System.Numerics.Vector4( 0.28f, 0.28f, 0.28f, 0.29f );
			colors[(int)ImGuiCol.SeparatorHovered] = new System.Numerics.Vector4( 0.44f, 0.44f, 0.44f, 0.29f );
			colors[(int)ImGuiCol.SeparatorActive] = new System.Numerics.Vector4( 0.40f, 0.44f, 0.47f, 1.00f );
			colors[(int)ImGuiCol.ResizeGrip] = new System.Numerics.Vector4( 0.28f, 0.28f, 0.28f, 0.29f );
			colors[(int)ImGuiCol.ResizeGripHovered] = new System.Numerics.Vector4( 0.44f, 0.44f, 0.44f, 0.29f );
			colors[(int)ImGuiCol.ResizeGripActive] = new System.Numerics.Vector4( 0.40f, 0.44f, 0.47f, 1.00f );
			colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.52f );
			colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4( 0.14f, 0.14f, 0.14f, 1.00f );
			colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4( 0.20f, 0.20f, 0.20f, 0.36f );
			colors[(int)ImGuiCol.TabUnfocused] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.52f );
			colors[(int)ImGuiCol.TabUnfocusedActive] = new System.Numerics.Vector4( 0.14f, 0.14f, 0.14f, 1.00f );
			colors[(int)ImGuiCol.DockingPreview] = new System.Numerics.Vector4( 0.33f, 0.67f, 0.86f, 1.00f );
			colors[(int)ImGuiCol.DockingEmptyBg] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.PlotLines] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.PlotLinesHovered] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.PlotHistogram] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.PlotHistogramHovered] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.TableHeaderBg] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.52f );
			colors[(int)ImGuiCol.TableBorderStrong] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.52f );
			colors[(int)ImGuiCol.TableBorderLight] = new System.Numerics.Vector4( 0.28f, 0.28f, 0.28f, 0.29f );
			colors[(int)ImGuiCol.TableRowBg] = new System.Numerics.Vector4( 0.00f, 0.00f, 0.00f, 0.00f );
			colors[(int)ImGuiCol.TableRowBgAlt] = new System.Numerics.Vector4( 1.00f, 1.00f, 1.00f, 0.06f );
			colors[(int)ImGuiCol.TextSelectedBg] = new System.Numerics.Vector4( 0.20f, 0.22f, 0.23f, 1.00f );
			colors[(int)ImGuiCol.DragDropTarget] = new System.Numerics.Vector4( 0.33f, 0.67f, 0.86f, 1.00f );
			colors[(int)ImGuiCol.NavHighlight] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 1.00f );
			colors[(int)ImGuiCol.NavWindowingHighlight] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 0.70f );
			colors[(int)ImGuiCol.NavWindowingDimBg] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 0.20f );
			colors[(int)ImGuiCol.ModalWindowDimBg] = new System.Numerics.Vector4( 1.00f, 0.00f, 0.00f, 0.35f );

			style.WindowPadding = new System.Numerics.Vector2( 8.00f, 8.00f );
			style.FramePadding = new System.Numerics.Vector2( 5.00f, 2.00f );
			style.CellPadding = new System.Numerics.Vector2( 6.00f, 6.00f );
			style.ItemSpacing = new System.Numerics.Vector2( 6.00f, 6.00f );
			style.ItemInnerSpacing = new System.Numerics.Vector2( 6.00f, 6.00f );
			style.TouchExtraPadding = new System.Numerics.Vector2( 0.00f, 0.00f );
			style.IndentSpacing = 25;
			style.ScrollbarSize = 15;
			style.GrabMinSize = 10;
			style.WindowBorderSize = 1;
			style.ChildBorderSize = 1;
			style.PopupBorderSize = 1;
			style.FrameBorderSize = 1;
			style.TabBorderSize = 1;
			style.WindowRounding = 7;
			style.ChildRounding = 4;
			style.FrameRounding = 3;
			style.PopupRounding = 4;
			style.ScrollbarRounding = 9;
			style.GrabRounding = 3;
			style.LogSliderDeadzone = 4;
			style.TabRounding = 4;
		}

		/// <summary>
		/// Frees all graphics resources used by the renderer.
		/// </summary>
		public void Dispose()
		{
			FontTexture.Unbind();
			Shader.Unbind();
		}
	}
}
