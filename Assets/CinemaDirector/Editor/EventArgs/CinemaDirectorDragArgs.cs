using System;

namespace CinemaDirector
{
	public class CinemaDirectorDragArgs : EventArgs
	{
		public DirectorObject cutscene;

		public UnityEngine.Object[] references;

		public CinemaDirectorDragArgs(DirectorObject cutscene, UnityEngine.Object[] references)
		{
			this.cutscene = cutscene;
			this.references = references;
		}
	}
}