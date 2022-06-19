using System;

namespace CinemaDirector
{
	public class CurveClipWrapperEventArgs : EventArgs
	{
		public CinemaClipCurveWrapper wrapper;

		public CurveClipWrapperEventArgs(CinemaClipCurveWrapper wrapper)
		{
			this.wrapper = wrapper;
		}
	}
	
}
