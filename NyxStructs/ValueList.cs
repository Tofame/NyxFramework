namespace NyxStructs;

public struct ValueList<T>
{
	private T[]? _items;
	private int _count;

	public readonly int Count => _count;
	public readonly int Capacity => _items?.Length ?? 0;

	public readonly T this[int index]
	{
		get
		{
			if (index < 0 || index >= _count)
				throw new System.ArgumentOutOfRangeException(nameof(index));
			return _items![index];
		}
	}

	public void Add(T item)
	{
		if (_items == null)
		{
			_items = new T[4];
		}
		else if (_count == _items.Length)
		{
			var newArr = new T[_items.Length * 2];
			System.Array.Copy(_items, newArr, _count);
			_items = newArr;
		}
		_items[_count++] = item;
	}

	public bool Remove(T item)
	{
		if (_items == null || _count == 0)
			return false;

		int index = System.Array.IndexOf(_items, item, 0, _count);
		if (index < 0)
			return false;

		RemoveAt(index);
		return true;
	}

	public void RemoveAt(int index)
	{
		if (_items == null || index < 0 || index >= _count)
			throw new System.ArgumentOutOfRangeException(nameof(index));

		int len = _count;
		if (index < len - 1)
		{
			System.Array.Copy(_items, index + 1, _items, index, len - 1 - index);
		}
		_items[--_count] = default!;
	}

	public void Insert(int index, T item)
	{
		if (index < 0 || index > _count)
			throw new System.ArgumentOutOfRangeException(nameof(index));

		if (_items == null)
		{
			_items = new T[4];
		}
		else if (_count == _items.Length)
		{
			var newArr = new T[_items.Length * 2];
			if (index > 0)
				System.Array.Copy(_items, 0, newArr, 0, index);
			newArr[index] = item;
			System.Array.Copy(_items, index, newArr, index + 1, _count - index);
			_items = newArr;
			_count++;
			return;
		}

		System.Array.Copy(_items, index, _items, index + 1, _count - index);
		_items[index] = item;
		_count++;
	}

	public void SetAt(int index, T item)
	{
		if (_items == null || index < 0 || index >= _count)
			throw new System.ArgumentOutOfRangeException(nameof(index));
		_items[index] = item;
	}

	public readonly ArraySegment<T> AsSpan()
	{
		return _items != null ? new ArraySegment<T>(_items, 0, _count) : ArraySegment<T>.Empty;
	}

	public readonly Enumerator GetEnumerator() => new Enumerator(this);

	public struct Enumerator
	{
		private readonly T[]? _items;
		private readonly int _count;
		private int _index;

		internal Enumerator(in ValueList<T> list)
		{
			_items = list._items;
			_count = list._count;
			_index = -1;
		}

		public bool MoveNext()
		{
			_index++;
			return _index < _count;
		}

		public readonly T Current => _items![_index];
	}
}
