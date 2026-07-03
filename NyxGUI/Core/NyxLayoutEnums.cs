namespace NyxGui;

/// <summary>Stack/flow direction for layout engines.</summary>
public enum Orientation
{
	Vertical,
	Horizontal
}

/// <summary>Child alignment along the cross-axis of a layout.</summary>
public enum Alignment
{
	Start,
	Center,
	End,
	Stretch
}

/// <summary>Which edge a child docks to in Dock layout.</summary>
public enum Dock
{
	Top,
	Bottom,
	Left,
	Right,
	Fill
}

/// <summary>Specifies how an image/texture should fit inside its bounds.</summary>
public enum NyxObjectFit
{
	Fill,
	Contain,
	Cover,
	None,
	ScaleDown
}
