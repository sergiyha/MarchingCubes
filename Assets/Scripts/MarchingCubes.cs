
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


public class MarchingCubes : MonoBehaviour
{
	[Range(0, 1)] public float IsoLevel;

	[SerializeField]
	private IntVector3 _size;

	[SerializeField]
	private int _cellSize;

	[SerializeField] private MeshFilter _meshFilter;

	[SerializeField] private bool _withGizmos;


	private Vector4[] _points = null;
	private Cube[] _cubes = null;
	private List<Triangle> _triangles;

	private void Start()
	{
		_triangles = new List<Triangle>();
		InitPointsForSphere();
		InitCubes();
		MarchCubes();
		CombineTris();
	}

	private void InitPointsForSphere()
	{
		_points = new Vector4[_size.x * _size.y * _size.z];
		_cubes = new Cube[_size.x * _size.y * _size.z];
		var middlePoint = new Vector3((_size.x - 1) * _cellSize / 2f, (_size.y - 1) * _cellSize / 2f, (_size.z - 1) * _cellSize / 2f);
		var maxLength = _size.x * _cellSize / 2;

		for (int z = 0; z < _size.x; z++)
		{
			for (int y = 0; y < _size.y; y++)
			{
				for (int x = 0; x < _size.z; x++)
				{

					int index = IndxFromCoord(x, y, z);
					var currPoint = new Vector3(x * _cellSize, y * _cellSize, z * _cellSize);
					var w = 1 - Vector3.Distance(middlePoint, currPoint) / maxLength;
					_points[index] = new Vector4(currPoint.x, currPoint.y, currPoint.z, w);
				}
			}
		}
	}



	private void OnDrawGizmos()
	{
		if (!_withGizmos) return;
		if (_points == null || _points.Length == 0) return;
		for (int z = 0; z < _size.x; z++)
		{
			for (int y = 0; y < _size.y; y++)
			{
				for (int x = 0; x < _size.z; x++)
				{
					int index = IndxFromCoord(x, y, z);
					var point = _points[index];
					//if ((Vector3)_points[index] == Vector3.zero) Debug.LogError("Zero");
					if (IsoLevel < _points[index].w) continue;
					Gizmos.color = new Color(point.w, point.w, point.w);
					Gizmos.DrawCube(point, new Vector3(0.1f, 0.1f, 0.1f));
					//Handles.Label(point, $"X:{x} Y:{y} Z:{z} W:{_points[index].w}");
				}
			}
		}

		DrawTris();
	}

	private void DrawTris()
	{
		if (_triangles == null) return;
		foreach (var tri in _triangles)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawCube(tri.p1, new Vector3(0.05f, 0.05f, 0.05f));
			Gizmos.DrawCube(tri.p2, new Vector3(0.05f, 0.05f, 0.05f));
			Gizmos.DrawCube(tri.p3, new Vector3(0.05f, 0.05f, 0.05f));
		}
	}



	private void InitCubes()
	{
		for (int z = 0; z < _size.x; z++)
		{
			for (int y = 0; y < _size.y; y++)
			{
				for (int x = 0; x < _size.z; x++)
				{
					int index = IndxFromCoord(x, y, z);

					if (_size.z - 1 == z || _size.x - 1 == x || _size.y - 1 == y)//remove the max x y and z 
					{
						_cubes[index] = new Cube { Points = null };
						continue;
					}

					_cubes[index] = new Cube
					{
						Points = new[]
						{
							_points[IndxFromCoord(x, y, z)],
							_points[IndxFromCoord(x, y, z + 1)],
							_points[IndxFromCoord(x + 1, y, z + 1)],
							_points[IndxFromCoord(x + 1, y, z)],
							_points[IndxFromCoord(x, y + 1,z)],
							_points[IndxFromCoord(x, y + 1, z + 1)],
							_points[IndxFromCoord(x + 1, y + 1, z + 1)],
							_points[IndxFromCoord(x + 1, y + 1, z)]
						}
					};
				}
			}
		}
	}

	private void MarchCubes()
	{
		for (int i = 0; i < _cubes.Length; i++)
		{
			if (_cubes[i].Points == null) continue;

			CreateTris(_cubes[i]);
		}
	}

	private void CombineTris()
	{
		Mesh mesh = null;
		mesh = _meshFilter.mesh ? _meshFilter.mesh : new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		for (int i = 0; i < _triangles.Count; i++)
		{
			vertices.Add(_triangles[i].p1);
			vertices.Add(_triangles[i].p2);
			vertices.Add(_triangles[i].p3);
		}


		var triangles = new int[_triangles.Count * 3];
		for (var i = 0; i < triangles.Length; i++)
		{
			triangles[i] = i;
		}


		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles;
		_meshFilter.mesh = mesh;
		_meshFilter.mesh.RecalculateBounds();
		_meshFilter.mesh.RecalculateNormals();
	}

	private Triangle[] CreateTris(Cube cube)
	{
		int cubeTableIndex = 0;
		if (cube.Points[0].w < IsoLevel) cubeTableIndex |= 1;
		if (cube.Points[1].w < IsoLevel) cubeTableIndex |= 2;
		if (cube.Points[2].w < IsoLevel) cubeTableIndex |= 4;
		if (cube.Points[3].w < IsoLevel) cubeTableIndex |= 8;
		if (cube.Points[4].w < IsoLevel) cubeTableIndex |= 16;
		if (cube.Points[5].w < IsoLevel) cubeTableIndex |= 32;
		if (cube.Points[6].w < IsoLevel) cubeTableIndex |= 64;
		if (cube.Points[7].w < IsoLevel) cubeTableIndex |= 128;
		if (cubeTableIndex == 0) return null;

		Triangle[] triangles = new Triangle[5];

		int nTriang = 0;
		try
		{
			for (int i = 0; Table.TriTable[cubeTableIndex, i] != -1; i += 3)
			{
				triangles[nTriang].p1 = GetEdgeVertexPos(Table.TriTable[cubeTableIndex, i], cube);
				triangles[nTriang].p2 = GetEdgeVertexPos(Table.TriTable[cubeTableIndex, i + 1], cube);
				triangles[nTriang].p3 = GetEdgeVertexPos(Table.TriTable[cubeTableIndex, i + 2], cube);
				_triangles.Add(triangles[nTriang]);
				nTriang++;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}


		return triangles;
	}

	private bool TrisCalculated = false;


	private Vector3 GetEdgeVertexPos(int edge, Cube cube)
	{
		switch (edge)
		{
			case 0:
				return VertexInterp(cube.Points[0], cube.Points[1]);
			case 1:
				return VertexInterp(cube.Points[1], cube.Points[2]);
			case 2:
				return VertexInterp(cube.Points[2], cube.Points[3]);
			case 3:
				return VertexInterp(cube.Points[3], cube.Points[0]);
			case 4:
				return VertexInterp(cube.Points[4], cube.Points[5]);
			case 5:
				return VertexInterp(cube.Points[5], cube.Points[6]);
			case 6:
				return VertexInterp(cube.Points[6], cube.Points[7]);
			case 7:
				return VertexInterp(cube.Points[7], cube.Points[4]);
			case 8:
				return VertexInterp(cube.Points[0], cube.Points[4]);
			case 9:
				return VertexInterp(cube.Points[1], cube.Points[5]);
			case 10:
				return VertexInterp(cube.Points[2], cube.Points[6]);
			case 11:
				return VertexInterp(cube.Points[3], cube.Points[7]);
			default:
				return Vector3.zero;
		}
	}


	Vector3 VertexInterp(Vector4 p1, Vector4 p2)
	{
		if (Mathf.Abs(IsoLevel - p1.w) < 0.00001)
			return (p1);
		if (Mathf.Abs(IsoLevel - p2.w) < 0.00001)
			return (p2);
		if (Mathf.Abs(p1.w - p2.w) < 0.00001)
			return (p1);

		float mu = (IsoLevel - p1.w) / (p2.w - p1.w);
		Vector3 vertexPosition = p1 + mu * (p2 - p1);
		return vertexPosition;
	}

	private void OnDisable()
	{
		_points = null;
	}

	private int IndxFromCoord(int x, int y, int z)
	{
		return (z * _size.x * _size.y) + (y * _size.x) + x;
	}
}

[System.Serializable]
public struct Cube
{
	public Vector4[] Points;
}

[System.Serializable]
public struct IntVector3
{
	public int x, y, z;
}

public struct Triangle
{
	public Vector3 p1;
	public Vector3 p2;
	public Vector3 p3;
}
