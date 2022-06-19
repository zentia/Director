namespace CinemaDirector
{
	public enum CoordinateType
	{
		None = 0,       // 不使用坐标系
		Local = 1 << 0, // 本地坐标系
		World = 1 << 1, // 世界坐标系
		Coordinated = 1 << 2, // 相对坐标系
		Relative = 1 << 3, // 绝对坐标系
		NormalizeRelative = 1 << 4, // 归一化绝对坐标系
	}
}