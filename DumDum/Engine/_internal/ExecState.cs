using System;
using System.Linq;

namespace DumDum.Engine._internal
{
	public class ExecState
	{
		public TimeSpan _frameElapsed;
		public TimeSpan _totalElapsed;
		public int _totalFrames = 0;

		private float[] _lastFpsSamples = new float[100];

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
			_lastFpsSamples[_totalFrames % _lastFpsSamples.Length] = instantFps;
			_avgFps = _lastFpsSamples.Average();
			_maxFps= _lastFpsSamples.Max();
			_minFps= _lastFpsSamples.Min();

		}
	}
}