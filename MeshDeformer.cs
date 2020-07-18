using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LatticePoint {
	// 0-1 parallel to 3-2
	public Vector3 position;
	public Vector3 startingPosition;

	public LatticePoint(Vector3 position) {
		this.position = position;
		this.startingPosition = position;
	}

	public Vector3 GetOffset() {
		return position - startingPosition;
	}
}

public class MeshDeformer : MonoBehaviour {
	public Transform[] handles;

	private List<LatticePoint> lattice = new List<LatticePoint>();

	[Range(1, 20)]
	public int latticeDivisions = 10;

	private MeshFilter meshFilter;
	private Mesh mesh;

	public void Start() {
		this.meshFilter = this.GetComponent<MeshFilter>();
		this.mesh = this.meshFilter.sharedMesh;
		BuildLattice();
	}

	public void Update() {
		UpdateLattice();
		Deform();
	}

	public void OnDrawGizmos() {
		foreach (LatticePoint t in lattice) {
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(t.position, .03f);
		}
	}

	// Lerp across two parallel lines, adding a point at the start, end, and every lerped point in between.
	public void BuildLattice() {
		lattice.Clear();

		for (int i = 0; i < latticeDivisions + 2; i++) {
			Vector3 divisionStart = Vector3.Lerp(handles[0].position, handles[1].position, i / (latticeDivisions * 1f));
			Vector3 divisionEnd = Vector3.Lerp(handles[3].position, handles[2].position, i / (latticeDivisions * 1f));
			lattice.Add(new LatticePoint(divisionStart));
			lattice.Add(new LatticePoint(divisionEnd));

			for (int j = 1; j < latticeDivisions; j++) {
				lattice.Add(new LatticePoint(Vector3.Lerp(divisionStart, divisionEnd, j / (latticeDivisions * 1f))));
			}
		}
	}

	public void UpdateLattice() {
		for (int i = 0; i < latticeDivisions + 2; i++) {
			int rowStart = i * (latticeDivisions + 1);
			Vector3 divisionStart = Vector3.Lerp(handles[0].position, handles[1].position, i / (latticeDivisions * 1f));
			Vector3 divisionEnd = Vector3.Lerp(handles[3].position, handles[2].position, i / (latticeDivisions * 1f));
			lattice[rowStart].position = divisionStart;
			lattice[rowStart + 1].position = divisionEnd;

			for (int j = 1; j < latticeDivisions; j++) {
				lattice[rowStart + 1 + j].position = Vector3.Lerp(divisionStart, divisionEnd, j / (latticeDivisions * 1f));
			}
		}
	}

	public void Deform() {
		Mesh clonedMesh = new Mesh();
		clonedMesh.name = "deformedMesh";
		clonedMesh.vertices = mesh.vertices;
		clonedMesh.triangles = mesh.triangles;
		clonedMesh.normals = mesh.normals;
		clonedMesh.uv = mesh.uv;

		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;

		List<Vector3> deformedVertices = new List<Vector3>();

		// For every vertex in the original mesh...
		foreach (Vector3 vertex in vertices) { 


			// Find the closest point in the original lattice
			float lowestDistanceToLattice = Mathf.Infinity;
			Vector3 latticeOffset = Vector3.zero;
			foreach (LatticePoint l in lattice) {
				float dist = Vector3.Distance(l.startingPosition, vertex);
				if (dist < lowestDistanceToLattice) {
					lowestDistanceToLattice = dist;
					latticeOffset = l.GetOffset();
				}
			}

			// Once found, any offset applied to that lattice should affect this vertex
			deformedVertices.Add(vertex + latticeOffset);
			
		}


		clonedMesh.vertices = deformedVertices.ToArray();
		this.meshFilter.sharedMesh = clonedMesh;
	}
}
