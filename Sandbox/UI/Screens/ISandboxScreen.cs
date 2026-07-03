using Silk.NET.Input;
using NyxRender;

namespace Sandbox.UI.Screens;

/// <summary>
/// Defines a screen state (e.g. main menu, gameplay) managed by SandboxApp.
/// </summary>
internal interface ISandboxScreen : IDisposable
{
	void OnEnter();
	void OnExit();
	void Update(double deltaTime, IInputContext input, int winW, int winH, bool blocksMovement);
	void Draw(double deltaTime, SpriteRenderer renderer, int winW, int winH);
	void Resize(int width, int height);
}
