﻿using UnityEngine;
using System.Collections;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class DepthMeshRenderer : MonoBehaviour
{
	[Tooltip("Target mesh width in meters.")]
	public float meshWidth = 5.12f;

	[Tooltip("Target mesh height in meters.")]
	public float meshHeight = 4.24f;


    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;


    private MeshCollider _meshCollider;

	public float meshUpdateInterval = 1f;

	//private Vector2[] colorCoords = null;
//	private ushort[] depthData = null;

	private int _depthWidth = 0;
	private int _depthHeight = 0;
	private int colorWidth = 0;
	private int colorHeight = 0;

	private const int SampleSize = 2;
	

    private float _lastTimeUpdatedMeshCollider = -999f;

    
    public void Init(int depthImageWidth, int depthImageHeight){
        _depthWidth = depthImageWidth;
        _depthHeight = depthImageHeight;

        


        
        InitMesh(_depthWidth / SampleSize, _depthHeight / SampleSize);        
    }

    private void InitMesh(int width, int height)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        _meshCollider = GetComponent<MeshCollider>();
        _meshCollider.sharedMesh = mesh;
        


        vertices = new Vector3[width * height];
        uvs = new Vector2[width * height];
        triangles = new int[6 * ((width - 1) * (height - 1))];

		float scaleX = meshWidth / width;
		float scaleY = meshHeight / height;

		float centerX = meshWidth / 2;
		float centerY = meshHeight / 2;

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

				float xScaled = x * scaleX - centerX;
				float yScaled = y * scaleY - centerY;

				vertices[index] = new Vector3(xScaled, -yScaled, 0);
                uvs[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
    
    
    
    public void UpdateMesh(short[] rawDepthData)
    {
        

        for (int y = 0; y < _depthHeight; y += SampleSize)
        {
            for (int x = 0; x < _depthWidth; x += SampleSize)
            {
                int indexX = x / SampleSize;
                int indexY = y / SampleSize;
                int smallIndex = (indexY * (_depthWidth / SampleSize)) + indexX;
                
                float avg = GetAvg(rawDepthData, x, y);
                vertices[smallIndex].z = avg;
                
                // Update UV mapping with CDRP                
                //uvs[smallIndex] = new Vector2(colorCoord.x / colorWidth, colorCoord.y / colorHeight);
            }
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        if(Time.time - _lastTimeUpdatedMeshCollider > meshUpdateInterval){
            _meshCollider.sharedMesh = mesh;
            _lastTimeUpdatedMeshCollider = Time.time;
        }
        
        
		
        
    }
    
    private float GetAvg(short[] depthData, int x, int y)
    {
        float sum = 0f;
        
		for (int y1 = y; y1 < y + SampleSize; y1++)
        {
			for (int x1 = x; x1 < x + SampleSize; x1++)
            {
                int fullIndex = (y1 * _depthWidth) + x1;
                
                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];
            }
        }

		return sum / (1000f * SampleSize * SampleSize);
    }

}
