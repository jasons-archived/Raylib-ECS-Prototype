using DumDum.Bcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumDum.Engine.Ecs;

public unsafe struct TimeStats
{
	public TimeSpan _frameElapsed;
	public TimeSpan _totalElapsed;
	public int _frameId = 0;

	private const int SAMPLE_COUNT = 100;
	//private fixed float _lastFpsSamples[SAMPLE_COUNT];
	private fixed float _elapsedSamples[SAMPLE_COUNT];

	public float _avgMs = 0;
	public float _minMs = 0;
	public float _maxMs = 0;
	internal void Update(TimeSpan frameElapsed)
	{
		//simple fps metrics calculated

		_frameElapsed = frameElapsed;
		_totalElapsed = _totalElapsed.Add(frameElapsed);
		_frameId++;
		//var instantFps = (float)(1f / frameElapsed.TotalSeconds);
		var elapsedMs =(float) frameElapsed.TotalMilliseconds._Round_Generic(2);

		//TODO: instead of doing calculations on last 100 frames, store avg/min/max for last 10 seconds.

		fixed (float* ptr = _elapsedSamples)
		{
			var samples = new Span<float>(ptr, SAMPLE_COUNT);

			samples[_frameId % samples.Length] = elapsedMs;
			_avgMs = samples._Avg_Generic();
			_maxMs = samples._Max_Generic();
			_minMs = samples._Min_Generic();
		}
		var lst = new List<float>();
		
		

	}
	public override string ToString()
	{
		var gcTime = GC.GetGCMemoryInfo().PauseDurations._Sum_Generic();
		var frameInfo = $"frame= {_frameId} @ {_totalElapsed.TotalSeconds._Round_Generic(0)}sec ";
		var historyInfo = $" history = {_frameElapsed.TotalMilliseconds._Round_Generic(2)}cur {_maxMs._Round_Generic(1)}max {_avgMs._Round_Generic(1)}avg {_minMs._Round_Generic(1)}min  ";
		var gcInfo = $" GC={GC.CollectionCount(0)} ({gcTime.TotalMilliseconds._Round_Generic(1)} ms)";

		return frameInfo + historyInfo + gcInfo;
	}
	//TODO: add stopwatch showing current frame execution time
}


public enum FrameStatus
{
	NONE,
	SCHEDULED,
	PENDING,
	RUNNING,
	/// <summary>
	/// this node update() method completed, but it's children nodes ative (this frame) are not yet known to be finished.
	/// </summary>
	FINISHED_WAITING_FOR_CHILDREN,
	/// <summary>
	/// this node and all children (active this frame) are finished
	/// </summary>
	HIERARCHY_FINISHED,
}
