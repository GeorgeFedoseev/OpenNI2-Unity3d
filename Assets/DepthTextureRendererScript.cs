using System;
using System.Collections;
using System.Collections.Generic;
using OpenNIWrapper;
using UnityEngine;
using UnityEngine.UI;

public class DepthTextureRendererScript : MonoBehaviour
{

    private Color32[] _depthMapPixels;
    private Texture2D _depthMapTexture;

    // histogram stuff
    private float[] depthHistogramMap;

    private int _xRes, _yRes;

    private float _factor = 1f;

    // output
    [SerializeField]
    private RawImage _rawImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // METHODS
    public void Init(int xRes, int yRes, int maxPixelValue) {
        _xRes = xRes;
        _yRes = yRes;

        _depthMapPixels = new Color32[(int)(xRes/_factor) * (int)(yRes/_factor)];
        _depthMapTexture = new Texture2D((int)(xRes/_factor), (int)(yRes/_factor));

        // histogram        
        depthHistogramMap = new float[maxPixelValue];

        if(_rawImage != null){
            _rawImage.texture = _depthMapTexture;
        }
    }


    public void UpdateWithDepthData(short[] rawDepthMap){
        UpdateHistogram(rawDepthMap);
        UpdateDepthmapTexture(rawDepthMap);
    }


    void UpdateHistogram(short[] rawDepthMap)
	{
		int i, numOfPoints = 0;
		
		Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        for (i = 0; i < rawDepthMap.Length; i++) {
            // only calculate for valid depth
            if (rawDepthMap[i] != 0) {
                depthHistogramMap[rawDepthMap[i]]++;
                numOfPoints++;
            }
        }
		
        if (numOfPoints > 0) {
            for (i = 1; i < depthHistogramMap.Length; i++) {   
		        depthHistogramMap[i] += depthHistogramMap[i-1];
	        }
            for (i = 0; i < depthHistogramMap.Length; i++) {
                depthHistogramMap[i] = (1.0f - (depthHistogramMap[i] / numOfPoints)) * 255;
	        }
        }
		depthHistogramMap[0] = 0;
	}

    void UpdateDepthmapTexture(short[] rawDepthMap)
    {
        int factor = 1;        

		// flip the depthmap as we create the texture		
		int XScaled = _xRes / factor;
        int YScaled = _yRes / factor;

		int i = XScaled*YScaled-XScaled;
		int depthIndex = 0;
		for (int y = 0; y < YScaled; ++y, i-=XScaled)
		{
			for (int x = XScaled-1; x >= 0; --x, depthIndex += factor)
			{
				short pixel = rawDepthMap[depthIndex];
                    
				if (pixel == 0)
				{
					_depthMapPixels[i+x] = Color.clear;
				}
				else
				{
					Color32 c = new Color32(0, (byte)depthHistogramMap[pixel], 0, 255);
					_depthMapPixels[i+x] = c;
                }
			}
            // Skip lines
			depthIndex += (factor-1)*_xRes; 
		}		

		_depthMapTexture.SetPixels32(_depthMapPixels);
        _depthMapTexture.Apply();      
   }
}
