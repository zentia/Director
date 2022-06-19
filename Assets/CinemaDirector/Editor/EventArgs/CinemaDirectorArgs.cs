using System;

namespace CinemaDirector
{
	public class CinemaDirectorArgs : EventArgs
	{
		public float timeArg;

		public DirectorObject cutscene;

		public CinemaDirectorArgs(DirectorObject cutscene)
		{
			this.cutscene = cutscene;
		}

		public CinemaDirectorArgs(DirectorObject cutscene, float time)
		{
			this.cutscene = cutscene;
			timeArg = time;
		}
	}
}