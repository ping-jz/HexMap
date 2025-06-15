using UnityEngine;

public static class Bezier
{
	//曲綫
	public static Vector3 GetPoint(Vector3 a, Vector3 b, Vector3 c, float t)
	{
		float r = 1f - t;
		return r * r * a + 2f * r * t * b + t * t * c;
	}

	//曲綫的倒數，好像可以求方向
	public static Vector3 GetDerivative(
		Vector3 a, Vector3 b, Vector3 c, float t
	)
	{
		return 2f * ((1f - t) * (b - a) + t * (c - b));
	}
}