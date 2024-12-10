﻿using Drawie.Backend.Core.Vector;
using Drawie.Numerics;

namespace PixiEditor.Views.Overlays.PathOverlay;

public class EditableVectorPath
{
    private VectorPath path;

    public VectorPath Path
    {
        get => path;
        set
        {
            UpdatePathFrom(value);
            path = value;            
        }
    }

    private List<SubShape> subShapes = new List<SubShape>();

    public IReadOnlyList<SubShape> SubShapes => subShapes;
    
    public int TotalPoints => subShapes.Sum(x => x.Points.Count);

    public int ControlPointsCount
    {
        get
        {
            // count verbs with control points
            return subShapes.Sum(x => CountControlPoints(x.Points));
        }
    }

    public EditableVectorPath(VectorPath path)
    {
        if (path != null)
        {
            Path = new VectorPath(path);
            UpdatePathFrom(Path);
        }
        else
        {
            this.path = null;
        }
    }

    public VectorPath ToVectorPath()
    {
        VectorPath newPath = new VectorPath(Path);
        newPath.Reset(); // preserve fill type and other properties
        foreach (var subShape in subShapes)
        {
            AddVerbToPath(CreateMoveToVerb(subShape), newPath);
            for (int i = 0; i < subShape.Points.Count; i++)
            {
                AddVerbToPath(subShape.Points[i].Verb, newPath);
            }

            if (subShape.IsClosed)
            {
                newPath.Close();
            }
        }

        return newPath;
    }
    
    private static Verb CreateMoveToVerb(SubShape subShape)
    {
        VecF[] points = new VecF[4];
        points[0] = subShape.Points[0].Position;
        
        return new Verb((PathVerb.Move, points, 0));
    }

    private void UpdatePathFrom(VectorPath from)
    {
        subShapes.Clear();
        if (from == null)
        {
            path = null;
            return;
        }

        int currentSubShapeStartingIndex = 0;
        bool isSubShapeClosed = false;
        int globalVerbIndex = 0;

        List<ShapePoint> currentSubShapePoints = new List<ShapePoint>();

        foreach(var data in from)
        {
            if (data.verb == PathVerb.Done)
            {
                if (!isSubShapeClosed)
                {
                    subShapes.Add(new SubShape(currentSubShapePoints, isSubShapeClosed));
                }
            }
            else if (data.verb == PathVerb.Close)
            {
                isSubShapeClosed = true;
                VecF[] verbData = data.points.ToArray();
                if (currentSubShapePoints[^1].Verb.IsEmptyVerb())
                {
                    verbData[0] = currentSubShapePoints[^2].Verb.To;
                    verbData[1] = currentSubShapePoints[0].Verb.From;
                    if (verbData[0] != verbData[1])
                    {
                        AddVerb((PathVerb.Line, verbData, 0), currentSubShapePoints);
                    }

                    currentSubShapePoints.RemoveAt(currentSubShapePoints.Count - 1);
                }

                subShapes.Add(new SubShape(currentSubShapePoints, isSubShapeClosed));

                currentSubShapePoints.Clear();

                currentSubShapeStartingIndex = globalVerbIndex;
            }
            else
            {
                isSubShapeClosed = false;
                if (data.verb == PathVerb.Move)
                {
                    currentSubShapePoints.Clear();
                }
                else
                {
                    AddVerb(data, currentSubShapePoints);
                }
            }

            globalVerbIndex++;
        }
    }
    
    private void AddVerbToPath(Verb verb, VectorPath newPath)
    {
        if (verb.IsEmptyVerb())
        {
            return;
        }

        switch (verb.VerbType)
        {
            case PathVerb.Move:
                newPath.MoveTo(verb.From);
                break;
            case PathVerb.Line:
                newPath.LineTo(verb.To);
                break;
            case PathVerb.Quad:
                newPath.QuadTo(verb.ControlPoint1.Value, verb.To);
                break;
            case PathVerb.Cubic:
                newPath.CubicTo(verb.ControlPoint1.Value, verb.ControlPoint2.Value, verb.To);
                break;
            case PathVerb.Conic:
                newPath.ConicTo(verb.ControlPoint1.Value, verb.To, verb.ConicWeight);
                break;
            case PathVerb.Close:
                newPath.Close();
                break;
        }
    }

    private static void AddVerb((PathVerb verb, VecF[] points, float conicWeight) data,
        List<ShapePoint> currentSubShapePoints)
    {
        VecF point = data.points[0];
        int atIndex = Math.Max(0, currentSubShapePoints.Count - 1);
        bool indexExists = currentSubShapePoints.Count > atIndex;
        ShapePoint toAdd = new ShapePoint(point, atIndex, new Verb(data));
        if (!indexExists)
        {
            currentSubShapePoints.Add(toAdd);
        }
        else
        {
            currentSubShapePoints[atIndex] = toAdd;
        }


        VecF to = Verb.GetPointFromVerb(data);
        currentSubShapePoints.Add(new ShapePoint(to, atIndex + 1, new Verb()));
    }

    public SubShape GetSubShapeContainingIndex(int index)
    {
        int currentIndex = 0;
        foreach (var subShape in subShapes)
        {
            if (currentIndex + subShape.Points.Count > index)
            {
                return subShape;
            }

            currentIndex += subShape.Points.Count;
        }

        return null;
    }
    
    private int CountControlPoints(IReadOnlyList<ShapePoint> points)
    {
        int count = 0;
        foreach (var point in points)
        {
            if(point.Verb.VerbType != PathVerb.Cubic) continue; // temporarily only cubic is supported for control points
            if (point.Verb.ControlPoint1 != null)
            {
                count++;
            }

            if (point.Verb.ControlPoint2 != null)
            {
                count++;
            }
        }

        return count;
    }

    public int GetSubShapePointIndex(int globalIndex, SubShape subShapeContainingIndex)
    {
        int currentIndex = 0;
        foreach (var subShape in subShapes)
        {
            if (subShape == subShapeContainingIndex)
            {
                return globalIndex - currentIndex;
            }

            currentIndex += subShape.Points.Count;
        }

        return -1;
    }

    public int GetGlobalIndex(SubShape subShape, int pointIndex)
    {
        int currentIndex = 0;
        foreach (var shape in subShapes)
        {
            if (shape == subShape)
            {
                return currentIndex + pointIndex;
            }

            currentIndex += shape.Points.Count;
        }

        return -1;
    }
}
