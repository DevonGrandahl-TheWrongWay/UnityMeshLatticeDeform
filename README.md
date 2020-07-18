# UnityMeshLatticeDeform
A quick and *dirty* example of lattice/cage mesh deformation in Unity3D.
![Demo](https://i.imgur.com/4Kce4Zg.gif)

TheWrongWay focuses on **minimal** examples that can (and probably should) be expanded and improved upon for your project.

## Motivation
While trying to recreate Oskar Stålberg's [Townscaper](https://store.steampowered.com/app/1291340/Townscaper/) I needed a way to do 2D 'cage' mesh deformation, but couldn't find any info on how the algorithm works. I had to start from scratch and failed a couple times, so I thought I'd save you some trouble.

## The Algorithm
### "Lattice": Take four handles that define a quad and create NxN evenly-spaced nodes inside that quad. When the handles move, the location of these nodes should update to stay evenly distributed. Each node must track it's own current position and starting position. This can expanded to a 3D to create a "Cage" of nodes.

### When a mesh is placed within the lattice, each vertex of the mesh should find the closest point on the lattice and move along with it. i.e., if the closest point on the lattice moves 2 units up, the vertex should also move 2 units up relative to its own position.

## Getting Started

Let's start with a model, placed in the scene view.

We want to deform our model by moving corners of a quad around. Easy enough concept. We're going to need a couple Transforms to be our handles. Let's place them around our model and start from there.

```
  // 0-1 parallel to 3-2
  public Transform[] handles;
```
![Handles](https://i.imgur.com/u60jYos.gif)

Now we need to create a lattice between them. First, let's create a class LatticePoint that stores the current position and starting position of the point, so we can calculate the offset later. Then, let's create a list of LatticePoints and a number of divisions we want in our lattice. 

For each division D, use lerp to find the spot on the 0-1 line, and find the corresponding spot on the parallel 3-2 line. These are our START and END points on opposite sides of the quad. Add them to our LatticePoints list. Lastly, for each division D2, lerp along the the START to END line and add those points to the LatticePoint array.

```
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

Now we have our list of LatticePoints, evenly spaced inside our handles. Let's add some gizmos so we can see our work.

![Lattice](https://i.imgur.com/OvHs9zO.gif)
