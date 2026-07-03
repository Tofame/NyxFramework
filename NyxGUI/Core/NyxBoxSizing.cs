namespace NyxGui;

/// <summary>
/// Defines the box sizing behavior for layout calculations.
/// </summary>
public enum NyxBoxSizing
{
	/// <summary>Width and height apply to the content area only. Padding and border are added outside.</summary>
	ContentBox,

	/// <summary>Width and height include padding and border inside the defined size.</summary>
	BorderBox
}
