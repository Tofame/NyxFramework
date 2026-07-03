using System;
using Silk.NET.OpenGL;
using NyxGui;
using NyxGuiRender.Gl;

namespace NyxGuiRender;

public class NyxRenderSurface : NyxWidget, IDisposable
{
	private readonly GL _gl;
	private uint _fbo;
	private uint _rbo;
	private GuiTexture? _colorTexture;
	private int _surfaceWidth;
	private int _surfaceHeight;

	private int _prevFbo;
	private readonly int[] _prevViewport = new int[4];

	public NyxRenderSurface(GL gl, int surfaceWidth, int surfaceHeight, string? id = null) : base(0)
	{
		_gl = gl;
		_surfaceWidth = surfaceWidth;
		_surfaceHeight = surfaceHeight;
		Id = id;
		InitializeFbo();
	}

	public int SurfaceWidth
	{
		get => _surfaceWidth;
		set
		{
			if (_surfaceWidth != value)
			{
				_surfaceWidth = value;
				InitializeFbo();
			}
		}
	}

	public int SurfaceHeight
	{
		get => _surfaceHeight;
		set
		{
			if (_surfaceHeight != value)
			{
				_surfaceHeight = value;
				InitializeFbo();
			}
		}
	}

	public uint Fbo => _fbo;
	public uint TextureHandle => _colorTexture?.Handle ?? 0;

	private void InitializeFbo()
	{
		CleanupFbo();

		if (_surfaceWidth <= 0 || _surfaceHeight <= 0)
			return;

		_fbo = _gl.GenFramebuffer();
		_gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

		// Color texture attachment
		_colorTexture = new GuiTexture(_gl, _surfaceWidth, _surfaceHeight, linearFilter: true);
		_gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTexture.Handle, 0);

		// Depth & Stencil attachment
		_rbo = _gl.GenRenderbuffer();
		_gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rbo);
		_gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, (uint)_surfaceWidth, (uint)_surfaceHeight);
		_gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);

		var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
		if (status != GLEnum.FramebufferComplete)
		{
			Console.WriteLine($"NyxRenderSurface FBO initialization failed. Status: {status}");
		}

		_gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
	}

	private void CleanupFbo()
	{
		if (_fbo != 0)
		{
			_gl.DeleteFramebuffer(_fbo);
			_fbo = 0;
		}
		if (_rbo != 0)
		{
			_gl.DeleteRenderbuffer(_rbo);
			_rbo = 0;
		}
		if (_colorTexture is not null)
		{
			_colorTexture.Dispose();
			_colorTexture = null;
		}
	}

	public void BeginRender()
	{
		if (_fbo == 0) return;

		// Save current framebuffer and viewport
		_gl.GetInteger(GLEnum.FramebufferBinding, out _prevFbo);
		_gl.GetInteger(GLEnum.Viewport, _prevViewport);

		// Bind FBO and set viewport to internal resolution
		_gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
		_gl.Viewport(0, 0, (uint)_surfaceWidth, (uint)_surfaceHeight);
	}

	public void EndRender()
	{
		// Restore previous framebuffer and viewport
		_gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)_prevFbo);
		_gl.Viewport(_prevViewport[0], _prevViewport[1], (uint)_prevViewport[2], (uint)_prevViewport[3]);
	}

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		if (_colorTexture is null || _fbo == 0) return;

		if (painter is NyxGuiRenderer renderer)
		{
			renderer.DrawRawTexture(_colorTexture, Bounds, Tint(NyxColor.FromRgb(255, 255, 255)));
		}
	}

	public void Dispose()
	{
		CleanupFbo();
		GC.SuppressFinalize(this);
	}
}
