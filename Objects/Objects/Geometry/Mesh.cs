﻿using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Objects.Geometry
{
  public class Mesh : Base, IHasBoundingBox, IHasVolume, IHasArea
  {
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> vertices { get; set; } = new List<double>();
    
    [DetachProperty]
    [Chunkable(62500)]
    public List<int> faces { get; set; } = new List<int>();

    /// <summary> Vertex colors as ARGB <see cref="int"/>s</summary>
    [DetachProperty]
    [Chunkable(62500)]
    public List<int> colors { get; set; } = new List<int>();

    [DetachProperty]
    [Chunkable(31250)]
    public List<double> textureCoordinates { get; set; } = new List<double>();

    public Box bbox { get; set; }

    public double area { get; set; }

    public double volume { get; set; }

    public string units { get; set; }

    public Mesh()
    {

    }

    public Mesh(double[] vertices, int[] faces, int[] colors = null, double[] texture_coords = null, string units = Units.Meters, string applicationId = null)
    {
      this.vertices = vertices.ToList();
      this.faces = faces.ToList();
      this.colors = colors?.ToList();
      this.textureCoordinates = texture_coords?.ToList();
      this.applicationId = applicationId;
      this.units = units;
    }
    
    #region Conveniance Methods
    
    public int VerticesCount => vertices.Count / 3;
    public int TextureCoordinatesCount => textureCoordinates.Count / 2;

    /// <summary>
    /// Gets a vertex as a <see cref="Point"/> by <paramref name="index"/>
    /// </summary>
    /// <param name="index">The index of the vertex</param>
    /// <returns>Vertex as a <see cref="Point"/></returns>
    public Point GetPointAtIndex(int index)
    {
      index *= 3;
      return new Point(
        vertices[index++],
        vertices[index++], 
        vertices[index],
        units,
        applicationId
        );
    }
    
    /// <summary>
    /// If not already so, this method will align <see cref="Mesh.vertices"/>
    /// such that a vertex and its corresponding texture coordinates have the same index.
    /// This alignment is what is expected by most applications.<br/>
    /// </summary>
    /// <remarks>
    /// If the calling application expects 
    /// <code>vertices.count == textureCoordinates.count</code>
    /// Then this method should be called by the <c>MeshToNative</c> method before parsing <see cref="Mesh.vertices"/> and <see cref="Mesh.faces"/>
    /// to ensure compatibility with geometry originating from applications that map vertices to texture-coordinates using vertex instance index (rather than vertex index)
    ///
    /// The result of this process is a new <see cref="Mesh.vertices"/> list with no shared vertices (vertices shared between polygons)
    /// Also re-aligns vertex <see cref="Mesh.colors"/> the same way.
    /// </remarks>
    public void AlignVerticesWithTexCoordsByIndex()
    {
      if (textureCoordinates.Count == 0) return;
      if (TextureCoordinatesCount == VerticesCount) return; //Tex-coords already aligned as expected
      
      var facesUnique = new List<int>(faces.Count);
      var verticesUnique = new List<double>(TextureCoordinatesCount * 3);
      bool hasColors = colors.Count > 0;
      var colorsUnique = hasColors? new List<int>(TextureCoordinatesCount) : null;
      
      
      int nIndex = 0;
      while (nIndex < faces.Count)
      {
        int n = faces[nIndex];
        if (n < 3) n += 3; // 0 -> 3, 1 -> 4
        
        if (nIndex + n >= faces.Count) break; //Malformed face list
        
        facesUnique.Add(n);
        for (int i = 1; i <= n; i++)
        {
          int vertIndex = faces[nIndex + i];
          int xIndex = vertIndex * 3;
          int newVertIndex = verticesUnique.Count / 3;
          
          verticesUnique.Add(vertices[xIndex]);     //x
          verticesUnique.Add(vertices[xIndex + 1]); //y
          verticesUnique.Add(vertices[xIndex + 2]); //z
          
          colorsUnique?.Add(colors[vertIndex]);
          facesUnique.Add(newVertIndex);
        }
        
        nIndex += n + 1;
      }
      
      vertices = verticesUnique;
      colors = colorsUnique;
      faces = facesUnique;
    }
    
    #endregion
  }
}
