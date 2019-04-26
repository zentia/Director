using UnityEngine;

public class CinemaClipCurveWrapper : CinemaActionWrapper
{
	public CinemaMemberCurveWrapper[] MemberCurves = new CinemaMemberCurveWrapper[0];
	internal int RowCount
	{
		get
		{
			int num = 0;
			CinemaMemberCurveWrapper[] memberCurves = MemberCurves;
			for (int i = 0; i < memberCurves.Length; i++)
			{
				CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
				num++;
				if (cinemaMemberCurveWrapper.IsFoldedOut)
				{
					CinemaAnimationCurveWrapper[] animationCurves = cinemaMemberCurveWrapper.AnimationCurves;
					for (int j = 0; j < animationCurves.Length; j++)
					{
						CinemaAnimationCurveWrapper arg_2F_0 = animationCurves[j];
						num++;
					}
				}
			}
			return num;
		}
	}

	internal bool IsEmpty
	{
		get
		{
			return MemberCurves == null || MemberCurves.Length == 0;
		}
	}

	public CinemaClipCurveWrapper(Behaviour behaviour, float firetime, float duration) : base(behaviour, firetime, duration)
	{
	}

	internal void TranslateCurves(float amount)
	{
		Firetime += amount;
		CinemaMemberCurveWrapper[] memberCurves = MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
				if (amount > 0f)
				{
					for (int k = cinemaAnimationCurveWrapper.KeyframeCount - 1; k >= 0; k--)
					{
						Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
						Keyframe kf = new Keyframe(keyframe.time + amount, keyframe.value, keyframe.inTangent, keyframe.outTangent);
						kf.tangentMode=(keyframe.tangentMode);
						cinemaAnimationCurveWrapper.MoveKey(k, kf);
					}
				}
				else
				{
					for (int l = 0; l < cinemaAnimationCurveWrapper.KeyframeCount; l++)
					{
						Keyframe keyframe2 = cinemaAnimationCurveWrapper.GetKeyframe(l);
						Keyframe kf2 = new Keyframe(keyframe2.time + amount, keyframe2.value, keyframe2.inTangent, keyframe2.outTangent);
						kf2.tangentMode=(keyframe2.tangentMode);
						cinemaAnimationCurveWrapper.MoveKey(l, kf2);
					}
				}
			}
		}
	}

	internal void CropFiretime(float newFiretime)
	{
		if (newFiretime >= Firetime + base.Duration)
		{
			return;
		}
		if (newFiretime < base.Firetime)
		{
			this.updateKeyframeTime(base.Firetime, newFiretime);
		}
		else
		{
			CinemaMemberCurveWrapper[] memberCurves = MemberCurves;
			for (int i = 0; i < memberCurves.Length; i++)
			{
				CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
				for (int j = 0; j < animationCurves.Length; j++)
				{
					animationCurves[j].CollapseStart(base.Firetime, newFiretime);
				}
			}
		}
		Duration += base.Firetime - newFiretime;
		base.Firetime = newFiretime;
	}

	internal void ScaleFiretime(float newFiretime)
	{
		if (newFiretime >= base.Firetime + base.Duration)
		{
			return;
		}
		CinemaMemberCurveWrapper[] memberCurves = this.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				animationCurves[j].ScaleStart(base.Firetime, base.Duration, newFiretime);
			}
		}
		base.Duration += base.Firetime - newFiretime;
		base.Firetime = newFiretime;
	}

	internal void CropDuration(float newDuration)
	{
		if (newDuration <= 0f)
		{
			return;
		}
		if (newDuration > base.Duration)
		{
			this.updateKeyframeTime(base.Firetime + base.Duration, base.Firetime + newDuration);
		}
		else
		{
			CinemaMemberCurveWrapper[] memberCurves = this.MemberCurves;
			for (int i = 0; i < memberCurves.Length; i++)
			{
				CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
				for (int j = 0; j < animationCurves.Length; j++)
				{
					animationCurves[j].CollapseEnd(base.Firetime + base.Duration, base.Firetime + newDuration);
				}
			}
		}
		Duration = newDuration;
	}

	internal void ScaleDuration(float newDuration)
	{
		if (newDuration <= 0f)
		{
			return;
		}
		CinemaMemberCurveWrapper[] memberCurves = this.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				animationCurves[j].ScaleEnd(base.Duration, newDuration);
			}
		}
		base.Duration = newDuration;
	}

	internal void ExtendDuration(float newDuration)
	{
		if (newDuration <= 0f)
		{
			return;
		}
		base.Duration = newDuration;
	}

	internal void Flip()
	{
		CinemaMemberCurveWrapper[] memberCurves = MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				animationCurves[j].Flip();
			}
		}
	}

	private void updateKeyframeTime(float oldTime, float newTime)
	{
		CinemaMemberCurveWrapper[] memberCurves = this.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaAnimationCurveWrapper[] animationCurves = memberCurves[i].AnimationCurves;
			for (int j = 0; j < animationCurves.Length; j++)
			{
				CinemaAnimationCurveWrapper cinemaAnimationCurveWrapper = animationCurves[j];
				for (int k = 0; k < cinemaAnimationCurveWrapper.KeyframeCount; k++)
				{
					Keyframe keyframe = cinemaAnimationCurveWrapper.GetKeyframe(k);
					if ((double)Mathf.Abs(keyframe.time - oldTime) < 1E-05)
					{
						Keyframe kf = new Keyframe(newTime, keyframe.value, keyframe.inTangent, keyframe.outTangent);
						kf.tangentMode=(keyframe.tangentMode);
						cinemaAnimationCurveWrapper.MoveKey(k, kf);
					}
				}
			}
		}
	}

	public bool TryGetValue(string type, string propertyName, out CinemaMemberCurveWrapper memberWrapper)
	{
		memberWrapper = null;
		CinemaMemberCurveWrapper[] memberCurves = this.MemberCurves;
		for (int i = 0; i < memberCurves.Length; i++)
		{
			CinemaMemberCurveWrapper cinemaMemberCurveWrapper = memberCurves[i];
			if (cinemaMemberCurveWrapper.Type == type && cinemaMemberCurveWrapper.PropertyName == propertyName)
			{
				memberWrapper = cinemaMemberCurveWrapper;
				return true;
			}
		}
		return false;
	}
}
