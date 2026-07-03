namespace Sandbox.UI;

/// <summary>
/// EXP rates for the analyzer: rolling current EXP/hr (~60s window) and session-average EXP/hr.
/// </summary>
internal sealed class ExpAnalyzerTracker
{
    /// <summary>Rolling window for “current” rate.</summary>
    private const int RollingWindowMs = 60_000;

    /// <summary>Current-rate graph: one sample per second, ~60s of history.</summary>
    private const int CurrentSampleIntervalMs = 1_000;
    private const int CurrentMaxSamples = 60;

    /// <summary>Session-average graph: one sample every 15 minutes.</summary>
    private const int SessionAvgSampleIntervalMs = 15 * 60_000;
    private const int SessionAvgMaxSamples = 48;

    private readonly Queue<(long TickMs, int Amount)> _gains = new();
    private readonly float[] _currentHrSamples = new float[CurrentMaxSamples];
    private readonly float[] _sessionAvgHrSamples = new float[SessionAvgMaxSamples];
    private int _currentCount;
    private int _currentHead;
    private int _sessionAvgCount;
    private int _sessionAvgHead;
    private int _sessionTotal;
    private long _sessionStartMs;
    private long _lastCurrentSampleMs;
    private long _lastSessionAvgSampleMs;
    private bool _sessionStarted;

    public int SessionTotal => _sessionTotal;

    public void AddGain(int amount, long tickMs = 0)
    {
        if (amount <= 0)
            return;

        if (tickMs <= 0)
            tickMs = Environment.TickCount64;

        if (!_sessionStarted)
        {
            _sessionStarted = true;
            _sessionStartMs = tickMs;
            RecordSessionAvgSample(tickMs);
        }

        _sessionTotal += amount;
        _gains.Enqueue((tickMs, amount));
        Prune(tickMs);
    }

    /// <summary>Call each frame; updates graph sample buffers.</summary>
    public void Tick(long nowMs = 0)
    {
        if (nowMs <= 0)
            nowMs = Environment.TickCount64;

        if (!_sessionStarted)
            return;

        Prune(nowMs);

        if (_currentCount > 0)
        {
            var lastIdx = (_currentHead - 1 + CurrentMaxSamples) % CurrentMaxSamples;
            _currentHrSamples[lastIdx] = (float)CurrentExpPerHour(nowMs);
        }

        if (_lastCurrentSampleMs == 0 || nowMs - _lastCurrentSampleMs >= CurrentSampleIntervalMs)
        {
            _lastCurrentSampleMs = nowMs;
            RecordCurrentSample(nowMs);
        }

        if (_lastSessionAvgSampleMs == 0 || nowMs - _lastSessionAvgSampleMs >= SessionAvgSampleIntervalMs)
        {
            _lastSessionAvgSampleMs = nowMs;
            RecordSessionAvgSample(nowMs);
        }
    }

    /// <summary>EXP/hr from gains in the last ~60 seconds.</summary>
    public double CurrentExpPerHour(long nowMs = 0)
    {
        if (nowMs <= 0)
            nowMs = Environment.TickCount64;

        Prune(nowMs);
        var cutoff = nowMs - RollingWindowMs;
        var sum = 0;
        foreach (var (tick, amount) in _gains)
        {
            if (tick >= cutoff)
                sum += amount;
        }

        return sum * (3_600_000.0 / RollingWindowMs);
    }

    /// <summary>Session total EXP divided by elapsed time.</summary>
    public double SessionAverageExpPerHour(long nowMs = 0)
    {
        if (nowMs <= 0)
            nowMs = Environment.TickCount64;

        if (!_sessionStarted)
            return 0;

        var elapsedMs = nowMs - _sessionStartMs;
        if (elapsedMs < 1_000)
            return 0;

        return _sessionTotal * (3_600_000.0 / elapsedMs);
    }

    public IReadOnlyList<float> GetCurrentExpPerHourSeries() => CopySeries(_currentHrSamples, _currentCount, _currentHead, CurrentMaxSamples);

    public IReadOnlyList<float> GetSessionAverageExpPerHourSeries() =>
        CopySeries(_sessionAvgHrSamples, _sessionAvgCount, _sessionAvgHead, SessionAvgMaxSamples);

    private void RecordCurrentSample(long tickMs)
    {
        _currentHrSamples[_currentHead] = (float)CurrentExpPerHour(tickMs);
        _currentHead = (_currentHead + 1) % CurrentMaxSamples;
        if (_currentCount < CurrentMaxSamples)
            _currentCount++;
    }

    private void RecordSessionAvgSample(long tickMs)
    {
        _sessionAvgHrSamples[_sessionAvgHead] = (float)SessionAverageExpPerHour(tickMs);
        _sessionAvgHead = (_sessionAvgHead + 1) % SessionAvgMaxSamples;
        if (_sessionAvgCount < SessionAvgMaxSamples)
            _sessionAvgCount++;
    }

    private static float[] CopySeries(float[] buffer, int count, int head, int capacity)
    {
        if (count == 0)
            return [];

        var result = new float[count];
        var start = (head - count + capacity) % capacity;
        for (var i = 0; i < count; i++)
            result[i] = buffer[(start + i) % capacity];
        return result;
    }

    private void Prune(long nowMs)
    {
        var cutoff = nowMs - RollingWindowMs;
        while (_gains.Count > 0 && _gains.Peek().TickMs < cutoff)
            _gains.Dequeue();
    }
}
