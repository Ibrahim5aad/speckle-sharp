using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Point = Objects.Geometry.Point;
using RH = Rhino.DocObjects;
using Rhino;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public BlockDefinition BlockDefinitionToSpeckle(RH.InstanceDefinition definition)
    {
      var geometry = new List<Base>();
      foreach (var obj in definition.GetObjects())
      {
        if (CanConvertToSpeckle(obj))
        {
          Base converted = ConvertToSpeckle(obj);
          if (converted != null)
          {
            converted["Layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
            geometry.Add(converted);
          }
        }
      }

      var _definition = new BlockDefinition()
      {
        name = definition.Name,
        basePoint = PointToSpeckle(Point3d.Origin), // rhino by default sets selected block def base pt at world origin
        geometry = geometry,
        units = ModelUnits
      };

      return _definition;
    }

    public InstanceDefinition BlockDefinitionToNative(BlockDefinition definition)
    {
      // get modified definition name with commit info
      var commitInfo = GetCommitInfo();
      var blockName = $"{commitInfo} - {definition.name}";

      // see if block name already exists and return if so
      if (Doc.InstanceDefinitions.Find(blockName) is InstanceDefinition def)
        return def;

      // base point
      Point3d basePoint = PointToNative(definition.basePoint).Location;

      // geometry and attributes
      var geometry = new List<GeometryBase>();
      var attributes = new List<ObjectAttributes>();
      foreach (var geo in definition.geometry)
      {
        if (CanConvertToNative(geo))
        {
          var converted = ConvertToNative(geo) as GeometryBase;
          if (converted == null)
            continue;
          var layerName = $"{commitInfo}{Layer.PathSeparator}{geo["Layer"] as string}";
          int index = 1;
          if (layerName != null)
            GetLayer(Doc, layerName, out index, true);
          var attribute = new ObjectAttributes()
          {
            LayerIndex = index
          };
          geometry.Add(converted);
          attributes.Add(attribute);
        }
      }

      int definitionIndex = Doc.InstanceDefinitions.Add(blockName, string.Empty, basePoint, geometry, attributes);

      if (definitionIndex < 0)
        return null;

      return Doc.InstanceDefinitions[definitionIndex];
    }

    // Rhino convention seems to order the origin of the vector space last instead of first
    // This results in a transposed transformation matrix - may need to be addressed later
    public BlockInstance BlockInstanceToSpeckle(RH.InstanceObject instance)
    {
      var t = instance.InstanceXform;
      var transformArray = new double[] { 
        t.M00, t.M01, t.M02, t.M03, 
        t.M10, t.M11, t.M12, t.M13, 
        t.M20, t.M21, t.M22, t.M23, 
        t.M30, t.M31, t.M32, t.M33 };

      var def = BlockDefinitionToSpeckle(instance.InstanceDefinition);

      var _instance = new BlockInstance()
      {
        insertionPoint = PointToSpeckle(instance.InsertionPoint),
        transform = transformArray,
        blockDefinition = def,
        units = ModelUnits
      };

      return _instance;
    }

    public InstanceObject BlockInstanceToNative(BlockInstance instance)
    {
      // get the block definition
      InstanceDefinition definition = BlockDefinitionToNative(instance.blockDefinition);

      // get the transform
      if (instance.transform.Length != 16)
        return null;
      Transform transform = new Transform();
      int count = 0;
      for (int i = 0; i < 4; i++)
      {
        for (int j = 0; j < 4; j++)
        {
          if (j == 3 && i != 3) // scale the delta values for translation transformations
            transform[i, j] = ScaleToNative(instance.transform[count], instance.units);
          else
            transform[i, j] = instance.transform[count];
          count++;
        }
      }

      // create the instance
      if (definition == null)
        return null;
      Guid instanceId = Doc.Objects.AddInstanceObject(definition.Index, transform);

      if (instanceId == Guid.Empty)
        return null;

      return Doc.Objects.FindId(instanceId) as InstanceObject;
    }
  }
}