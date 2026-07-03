namespace NyxGui;

/// <summary>
/// Base class for custom layout engines (stack, grid, dock, wrap).
/// </summary>
public abstract class NyxLayout
{
	public abstract void Measure(NyxContainer container, NyxSize availableSize);
	public abstract void Arrange(NyxContainer container, NyxRect finalRect);
}
