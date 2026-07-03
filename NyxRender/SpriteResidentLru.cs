using System.Runtime.InteropServices;

namespace NyxRender;

/// <summary>
/// LRU eviction tracker for sprite atlas residency.
///
/// Uses a doubly-linked list with an embedded free-list on the same <c>_next</c> array:
/// while a node is live, <c>_next</c> points to the next younger node in the LRU order;
/// when a node is freed, <c>_next</c> chains it into a singly-linked free-list headed by
/// <c>_freeIndex</c>.  This avoids separate heap allocations for list nodes.
///
/// Capacity doubles on demand.  The <c>_idToNode</c> dictionary maps sprite ids to node
/// indices for O(1) touch/remove.
/// </summary>
internal sealed class SpriteResidentLru
{
    private int[] _next;
    private int[] _prev;
    private Dictionary<int, int> _idToNode;
    private int _head = -1;
    private int _tail = -1;
    private int _count;
    private int _freeIndex = -1;
    private int _highWaterMark;
    private int[] _keys;

    public SpriteResidentLru(int capacity = 8192)
    {
        _next = new int[capacity];
        _prev = new int[capacity];
        _keys = new int[capacity];
        Array.Fill(_next, -1);
        Array.Fill(_prev, -1);
        _idToNode = new Dictionary<int, int>(capacity);
    }

    public int Count => _count;

    public void Add(int spriteId)
    {
        if (_idToNode.Remove(spriteId, out var idx))
            Unlink(idx);

        idx = AllocNode();
        _keys[idx] = spriteId;
        _idToNode[spriteId] = idx;
        LinkHead(idx);
    }

    /// <summary>Marks <paramref name="spriteId"/> as most-recently-used.  Adds it if not present.</summary>
    public void Touch(int spriteId)
    {
        if (!_idToNode.TryGetValue(spriteId, out var idx))
        {
            Add(spriteId);
            return;
        }

        if (idx == _head)
            return;

        Unlink(idx);
        LinkHead(idx);
    }

    public void Remove(int spriteId)
    {
        if (!_idToNode.Remove(spriteId, out var idx))
            return;
        Unlink(idx);
        FreeNode(idx);
    }

    public bool TryPeekOldest(out int spriteId)
    {
        if (_tail < 0)
        {
            spriteId = 0;
            return false;
        }

        spriteId = _keys[_tail];
        return true;
    }

    /// <summary>Removes and returns the least-recently-used sprite id.</summary>
    public bool TryEvictOldest(out int spriteId)
    {
        if (!TryPeekOldest(out spriteId))
            return false;

        var idx = _tail;
        Unlink(idx);
        _idToNode.Remove(spriteId);
        FreeNode(idx);
        return true;
    }

    private void Unlink(int idx)
    {
        var p = _prev[idx];
        var n = _next[idx];

        if (p >= 0) _next[p] = n;
        else _head = n;

        if (n >= 0) _prev[n] = p;
        else _tail = p;

        _count--;
    }

    private void LinkHead(int idx)
    {
        _prev[idx] = -1;
        _next[idx] = _head;

        if (_head >= 0)
            _prev[_head] = idx;
        else
            _tail = idx;

        _head = idx;
        _count++;
    }

    /// <summary>
    /// Returns a node index, either from the free-list or by growing the arrays.
    /// Invariant: the total number of nodes ever allocated is <c>_count + _idToNode.Count</c>
    /// (live + freed).  When that index overflows the current capacity, arrays are doubled
    /// and the newly-added second half is filled with -1 sentinels.
    /// </summary>
    private int AllocNode()
    {
        // Pop from single-linked free-list stored in _next[]
        if (_freeIndex >= 0)
        {
            var idx = _freeIndex;
            _freeIndex = _next[_freeIndex];
            _next[idx] = -1;
            _prev[idx] = -1;
            return idx;
        }

        var newIdx = _highWaterMark;
        if (newIdx >= _next.Length)
        {
            var newCap = _next.Length * 2;
            Array.Resize(ref _next, newCap);
            Array.Resize(ref _prev, newCap);
            Array.Resize(ref _keys, newCap);
            Array.Fill(_next, -1, _next.Length / 2, _next.Length / 2);
            Array.Fill(_prev, -1, _prev.Length / 2, _prev.Length / 2);
        }

        _highWaterMark = newIdx + 1;
        return newIdx;
    }

    /// <summary>Pushes a node index onto the free-list for reuse.</summary>
    private void FreeNode(int idx)
    {
        _next[idx] = _freeIndex;
        _freeIndex = idx;
    }
}
