using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PolygonDrawer : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of sides in the polygon")] private int numberOfSides = 3;
    [SerializeField, Tooltip("The amount of 'lines' per line")] private int vertsPerLine = 4;
    [SerializeField, Tooltip("The length of each side and radius of circle")] private float radius = 5;
    [SerializeField, Tooltip("Density of line connections")] private int density = 1;
    [SerializeField, Tooltip("Scales the lines by distance from center")] private bool scaleByDistance = false;
    [SerializeField, Tooltip("Height of the shape")] private float height = 1;

    [SerializeField, Tooltip("The bottom color")] private Color startColor = Color.red;
    [SerializeField, Tooltip("The middle color")] private Color midColor = Color.blue;
    [SerializeField, Tooltip("The top color")] private Color endColor = Color.blue;



    [SerializeField, Tooltip("Doubles the edge loops, making it rounded")] private bool makeRounded = false;


    private List<List<Vector3>> outsideLoop;


    private void Start()
    {
        CreateShape();
    }


    public void OnSidesChanged(string input)
    {
        if (!int.TryParse(input, out int value))
            return;

        numberOfSides = value;
        CreateShape();
    }

    public void OnVertsChanged(string input)
    {
        if (!int.TryParse(input, out int value))
            return;

        vertsPerLine = value;
        CreateShape();
    }

    public void OnRadiusChanged(string input)
    {
        if (!float.TryParse(input, out float value))
            return;
        radius = value;
        CreateShape();
    }

    public void OnDensityChanged(string input)
    {
        if (!int.TryParse(input, out int value))
            return;
        density = value;
        CreateShape();
    }
    public void OnHeightChanged(string input)
    {
        if (!float.TryParse(input, out float value))
            return;
        height = value;
        CreateShape();
    }
    public void OnStartColorChanged(Color color) => startColor = color;

    public void OnMidColorChanged(Color color) => midColor = color;

    public void OnEndColorChanged(Color color) => endColor = color;

    public void OnScalingToggled(bool value)
    {
        scaleByDistance = value;
        CreateShape();
    }

    public void OnMakeRoundedPressed(bool value)
    {
        makeRounded = value;
        CreateShape();
    }

    private void CreateShape()
    {
        if (makeRounded)
        {
            MakeTorus();
        }
        else
        {
            PlacePointsV2();
        }
    }

    private void OnValidate()
    {
        if (outsideLoop?.Count > 0)
            outsideLoop.Clear();

        CreateShape();
    }

    //place points using maf
    private void PlacePointsV2()
    {
        outsideLoop = new List<List<Vector3>>();

        if (vertsPerLine == 0)
            vertsPerLine = 1;

        float offsetPerLine = height / vertsPerLine;
        float initialOffset = 0;

        if (vertsPerLine > 1)
            initialOffset = offsetPerLine * (vertsPerLine-1) / 2;

        Debug.Log($"{initialOffset}");

        for (int j = 0; j < vertsPerLine; j++)
        {
            List<Vector3> ring = new List<Vector3>();
            float yOffset = offsetPerLine * j;

            float dist = Mathf.Abs(-initialOffset + yOffset);

            for (int i = 0; i < numberOfSides; i++)
            {
                float angleRadians = Mathf.PI * 2 / numberOfSides; //the rotation amount in degrees (which is used by Quaternion.Euler)
                Vector3 point = transform.TransformPoint(GetPointOnLoop(angleRadians * i, dist, radius, -initialOffset + yOffset));
                ring.Add(point);
            }
            outsideLoop.Add(ring);
        }
    }

    private void MakeTorus()
    {
        outsideLoop = new List<List<Vector3>>();

        float ringRadius = 1f;

        for (int j = 0; j < numberOfSides; j++)
        {
            List<Vector3> ring = new List<Vector3>();

            for (int i = 0; i < vertsPerLine; i++)
            {
                float centerRadians = Mathf.PI * 2 / numberOfSides; //the rotation amount in degrees (which is used by Quaternion.Euler)
                float ringRadians = Mathf.PI * 2 / vertsPerLine;

                float x = Mathf.Cos(centerRadians*j) * (radius + ringRadius * Mathf.Cos(ringRadians*i));
                float y = ringRadius * Mathf.Sin(ringRadians * i);
                float z = Mathf.Sin(centerRadians * j) * (radius + ringRadius * Mathf.Cos(ringRadians * i));

                Vector3 point = new Vector3(x, y, z);
                Vector3 transformedPoint = transform.TransformPoint(point);
                
                ring.Add(transformedPoint);
            }
            outsideLoop.Add(ring);
        }
    }


    Vector3 GetPointOnLoop(float angleValue, float distanceFromCenter, float radius, float yOffset)
    {
        Vector3 point;
        if (scaleByDistance)
        {
            //cos and sin of the radians*i would place it along points on a 1-radius unit circle
            //multiplying by the length will make the circle larger, dividing the length by distance will make that circle smaller
            if (distanceFromCenter > 0)
            {
                point = new Vector3(Mathf.Cos(angleValue) * (radius / (1 + distanceFromCenter)), yOffset, Mathf.Sin(angleValue) * (radius / (1 + distanceFromCenter)));
            }
            else
                point = new Vector3(Mathf.Cos(angleValue) * radius, yOffset, Mathf.Sin(angleValue) * (radius));
        }
        else
            point = new Vector3(Mathf.Cos(angleValue) * radius, yOffset, Mathf.Sin(angleValue) * radius);

        return point;
    }

    static Material lineMaterial;
    static void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }


    private void OnRenderObject()
    {
        //if (drawUnitCircle)
            //Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius, lineThickness);

        if (outsideLoop == null)
            return;

        if (outsideLoop.Count == 0)
            return;

        if (density >= outsideLoop[0].Count)
            return;

        CreateLineMaterial();
        // Apply the line material
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        // Set transformation matrix for drawing to
        // match our transform
        GL.MultMatrix(transform.localToWorldMatrix);


        int midPoint = outsideLoop.Count / 2;

        // Draw lines
        GL.Begin(GL.LINES);

        for (int i = 0; i < outsideLoop.Count; i++)
        {
            List<Vector3> ring = outsideLoop[i];
            List<Vector3> nextRing = outsideLoop[(i + 1) % outsideLoop.Count];

            Color lineColor;
            
            if (midPoint > 0 && outsideLoop.Count > 2)
            {
                //We want lerping between 3 colors, find the first,second and last thirds and lerp
                int firstThird = outsideLoop.Count / 3;
                int secondThrid = outsideLoop.Count / 3 * 2;
                if (i < firstThird)
                    lineColor = Color.Lerp(startColor, midColor, (float)i / firstThird);
                else if (i < secondThrid)
                    lineColor = Color.Lerp(midColor, endColor, (float)(i-firstThird) / secondThrid);
                else
                    lineColor = Color.Lerp(endColor, startColor, (float)(i-secondThrid) / firstThird);

            }
            else
                lineColor = startColor;

            //Handles.color = lineColor;
            GL.Color(lineColor);

            for (int j = 0; j < ring.Count + density; j++)
            {
                //the final points should connect to the first ones based on density etc, use modulo connect
                int point = j % ring.Count;
                int pointToConnectTo = (j + density) % outsideLoop[i].Count;

                Vector3 r1P1 = ring[point];
                Vector3 r1P2 = ring[pointToConnectTo];

                //Handles.DrawLine(r1P1, r1P2, lineThickness);
                GL.Vertex3(r1P1.x, r1P1.y, r1P1.z);
                GL.Vertex3(r1P2.x, r1P2.y, r1P2.z);

                //torus shape connects to the next ring
                if (makeRounded)
                {
                    Vector3 r2P1 = nextRing[point];
                    GL.Vertex3(r1P1.x, r1P1.y, r1P1.z);
                    GL.Vertex3(r2P1.x, r2P1.y, r2P1.z);
                    //Handles.DrawLine(r1P1, r2P1, lineThickness);
                }
            }
        }
        GL.End();
        GL.PopMatrix();
    }
}