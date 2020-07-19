# Mesh Lattice Deform
A quick and dirty example of lattice/cage mesh deformation in Unity3D. This algorithm is pretty simple and it's easily extendable to a full 3D Mesh Cage Deformer.

![Demo](https://i.imgur.com/4Kce4Zg.gif)

TheWrongWay focuses on **minimal** examples that can (and probably should) be expanded and improved upon for your project.

## Motivation
While trying to recreate Oskar Stålberg's [Townscaper](https://store.steampowered.com/app/1291340/Townscaper/) I needed a way to do 2D 'cage' mesh deformation, but couldn't find any info on how the algorithm works. I had to start from scratch and failed a couple times, so I thought I'd save you some trouble.

## Prerequisites
Some basic Unity/coding experience. The intention is not to detail every click you need to make, but the general outline of the code and algorithm.

## The Algorithm
Lattice Definition: take four handles that define a quad and create NxN evenly-spaced nodes inside that quad. When the handles move, the location of these nodes should update to stay evenly distributed. Each node must track it's own current position and starting position. This can expanded to a 3D to create a "Cage" of nodes.

When a mesh is placed within the lattice, each vertex of the mesh should find the closest point on the lattice and move along with it. i.e., if the closest point on the lattice moves 2 units up, the vertex should also move 2 units up relative to its own position.



## Getting Started

### Creating The Lattice

Let's start with a model, placed in the scene view. Create a Script named MeshDeformer and attach it to that model. Remember to mark that model as Read/Write enabled!

We want to deform our model by moving corners of a quad around. Easy enough concept. We're going to need a couple Transforms to be our handles. Let's place them around our model, add a reference to them in our script, and start from there.

```
// 0-1 parallel to 3-2
public Transform[] handles;
```
![Handles](https://i.imgur.com/u60jYos.gif)

Now we need to create a lattice between them. First, let's create a LatticePoint class that stores the current position and starting position of the point so we can calculate the offset later. Then, let's create a list of LatticePoints and an int for the amount divisions we want in our lattice. 

```
[System.Serializable]
public class LatticePoint {
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
// 0-1 must be parallel to 2-3. i.e., assign the points in a circle, not across.
	public Transform[] handles;
	private List<LatticePoint> lattice = new List<LatticePoint>();

	[Range(1, 20)]
	public int latticeDivisions = 10;
[...]
```

For each division D, use lerp to find the spot on the 0-1 line, and find the corresponding spot on the parallel 3-2 line. These are our START and END points on opposite sides of the quad. Add them to our LatticePoints list. Lastly, for each division D2, lerp between the START and END points and add those points to the LatticePoint array.

```
[...]
public void Start() {
  BuildLattice();
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
```

Now we have our list of LatticePoints, evenly spaced inside our handles. Let's add some gizmos and press Play so we can see our work.
```
public void OnDrawGizmos() {
	foreach (LatticePoint t in lattice) {
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(t.position, .03f);
	}
}
```

![Lattice](https://i.imgur.com/OvHs9zO.gif)

The last step of the lattice is to update the nodes every frame to keep up with our handles. This looks a lot like the BuildLattice function, so it really *should* be generalized, but here at TheWrongWay we *strongly* prefer readability over decent coding practices.

```
public void Update() {
  UpdateLattice();
}

[...]

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
```

### Deforming the Mesh
Based on [CatLikeCoding's Mesh Deform writeup](https://catlikecoding.com/unity/tutorials/mesh-deformation/), lets set up some variables. We'll need a reference to the MeshFilter and lets create a private variable to store our Mesh.

```
[...]
private MeshFilter meshFilter;
private Mesh mesh;

public void Start() {
	this.meshFilter = this.GetComponent<MeshFilter>();
	this.mesh = this.meshFilter.sharedMesh;
	BuildLattice();
}
 ```
Alright, let's bring it all together in our Deform method.
First, lets clone the mesh so we don't do anything we can't take back. Then for every vertex in the original mesh, find the LatticePoint P with the closest startingPoint. Check how far that latticePoint has moved from it's starting position and apply that same offset to our vertex point. Update the MeshFilter to use this mesh/vertices, and we're done!

```
public void Update() {
	UpdateLattice();
	Deform();
}

[...]

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
```
Press play and move the handles around, with any luck the model should now deform based on the locations of the handles! Feel free to just use the finished file if you are having trouble piecing the ideas together.

![Demo](https://i.imgur.com/4Kce4Zg.gif)

I only needed a 2D lattice, but keep in mind that this could pretty easily be applied to make a full 3D Cage Deformer. You'd just have to add 4 more handles and generate/update more LatticePoints between all of them. I don't think the Deform() method would have to change at all!

## The Right Way
* *Coding Practices* This should really be broken apart into multiple classes and files (Lattice, Deformer, LatticePoint). 
* *Coding Practices* The structure of the Handles array is **super** janky (edge 0-1 must be parallel to edge 2-3). Do something better than that.
* *Performance* This whole thing could (and should) be sped up a bit by storing the closest LatticePoint for every vertex, since it should be the same every frame. Maybe just a LatticePoint array with an entry for every vertex?
