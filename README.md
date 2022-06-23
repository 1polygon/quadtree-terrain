A procedural quadtree terrain implementation in unity. It generates a heightmap using [FastNoiseLite](https://github.com/Auburn/FastNoiseLite) and calculates the vertex offsets and normals in the TerrainShader.

![alt text](https://i.imgur.com/y0x5Gil.png)

For the lod transitions all 16 possible grid meshes are being generated by skipping odd vertices on the edges if needed.
The quadtree is being traversed down based on the camera distance until a minimum size is reached. Terrain meshes are being scaled based on the current tree node size and will be replaced accordingly if a lod seam changes.

![alt text](https://i.imgur.com/yTnxBRX.png)