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
	public int _totalFrames = 0;

	private const int SAMPLE_COUNT = 100;
	private fixed float _lastFpsSamples[SAMPLE_COUNT];

	public float _avgFps = 0;
	public float _minFps = 0;
	public float _maxFps = 0;
	internal void Update(TimeSpan frameElapsed)
	{
		//simple fps metrics calculated

		_frameElapsed = frameElapsed;
		_totalElapsed = _totalElapsed.Add(frameElapsed);
		_totalFrames++;
		var instantFps = (float)(1f / frameElapsed.TotalSeconds);

		//TODO: instead of doing calculations on last 100 frames, store avg/min/max for last 10 seconds.

		fixed (float* ptr = _lastFpsSamples)
		{
			var samples = new Span<float>(ptr, SAMPLE_COUNT);

			_lastFpsSamples[_totalFrames % samples.Length] = instantFps;
			_avgFps = MathF.Round(samples._AVG(), 1);
			_maxFps = MathF.Round(samples._MAX(), 1);
			_minFps = MathF.Round(samples._MIN(), 1);
		}


	}
}