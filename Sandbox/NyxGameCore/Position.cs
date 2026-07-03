namespace NyxGameCore;

public struct Position
{
	public int X { get; set; }
	public int Y { get; set; }
	public int Z { get; set; }

	public static readonly Position Zero = new(0, 0, 0);

	public Position(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public readonly Position Translate(int dx, int dy, int dz = 0) => new(X + dx, Y + dy, Z + dz);

	public readonly int DistanceChebyshev(Position other)
	{
		return Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y)) + Math.Abs(Z - other.Z) * 8;
	}

	public readonly int DistanceManhattan(Position other)
	{
		return Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);
	}

	public readonly bool IsAdjacentTo(Position other)
	{
		return Z == other.Z && Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y)) <= 1;
	}

	public readonly Position GetNeighbor(int direction)
	{
		return direction switch
		{
			0 => Translate(0, -1),
			1 => Translate(1, 0),
			2 => Translate(0, 1),
			3 => Translate(-1, 0),
			4 => Translate(1, -1),
			5 => Translate(1, 1),
			6 => Translate(-1, 1),
			7 => Translate(-1, -1),
			_ => this
		};
	}

	public readonly int GetDirectionTo(Position target)
	{
		int dx = target.X - X;
		int dy = target.Y - Y;

		if (dx == 0 && dy == 0)
			return -1;

		if (Math.Abs(dx) > Math.Abs(dy))
			return dx < 0 ? 3 : 1;
		else
			return dy < 0 ? 0 : 2;
	}

	public static bool operator ==(Position left, Position right) => left.X == right.X && left.Y == right.Y && left.Z == right.Z;
	public static bool operator !=(Position left, Position right) => !(left == right);

	public override readonly bool Equals(object? obj) => obj is Position other && this == other;
	public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
	public override readonly string ToString() => $"({X}, {Y}, {Z})";
}
