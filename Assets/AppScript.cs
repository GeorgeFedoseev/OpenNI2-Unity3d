using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenNIWrapper;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System;

public class AppScript : MonoBehaviour
{

    public float _factor = 1f;
    public int _maxPixelValue = 5000;

    private Device _currentDevice;
    private VideoStream _depthVideoStream;


    [SerializeField]
    private DepthMeshRenderer _depthMeshRenderer;

    // depth frame data
    private short[] _rawDepthMap;
    private Color32[] _depthMapPixels;
    private Texture2D _depthMapTexture;

    // histogram stuff
    float[] depthHistogramMap;

    // output
    [SerializeField]
    private RawImage _rawImage;


    private List<Vector3> _pointPositions = null;


    // Start is called before the first frame update
    void Start()
    {        
        OpenNI.Initialize();        

        OpenNI.OnDeviceConnected += OnDeviceConnectionStateChanged;
        OpenNI.OnDeviceDisconnected += OnDeviceConnectionStateChanged;
        var devices = OpenNI.EnumerateDevices();

        Debug.Log($"Found {devices.Length} devices");

        // foreach (var d in devices) {
        //     Debug.Log(d.Name);
        // }

        // connect
        if(devices.Length > 0){            
            OnDeviceConnectionStateChanged(devices[0]);
        }      
    }    

    // METHODS
    private void ConnectDevice(DeviceInfo deviceInfo){
        print($"Connecting to device {deviceInfo.Name}");

        MaybeDisconnectCurrentDevice();

        _currentDevice = deviceInfo.OpenDevice();

        InitDepthVideoStreamForCurrentDevice();

        _depthVideoStream.OnNewFrame += OnNewDepthFrame;
        _depthVideoStream.Start();
    }

    private void MaybeDisconnectCurrentDevice(){
        if(_currentDevice == null){
            return;
        }

        _currentDevice.Close();
    }

    void InitDepthVideoStreamForCurrentDevice(){
        if(_depthVideoStream != null){
            _depthVideoStream.Stop();
        }

        _depthVideoStream = _currentDevice.CreateVideoStream(Device.SensorType.Depth);


        Debug.Log($"H_FOV: {_depthVideoStream.HorizontalFieldOfView/Math.PI*180}");
        Debug.Log($"V_FOV: {_depthVideoStream.VerticalFieldOfView/Math.PI*180}");

        var res = _depthVideoStream.VideoMode.Resolution;
        Debug.Log($"depth resolution: {res.Width}x{res.Height}");

        _depthMeshRenderer.Init(
            res.Width,
            res.Height,
            _depthVideoStream.HorizontalFieldOfView,
            _depthVideoStream.VerticalFieldOfView
        );

        _rawDepthMap = new short[(int)(res.Width * res.Height)];
        _depthMapPixels = new Color32[(int)(res.Width/_factor) * (int)(res.Height/_factor)];
        _depthMapTexture = new Texture2D((int)(res.Width/_factor), (int)(res.Height/_factor));

        int maxDepth = (int)_depthVideoStream.MaxPixelValue;
        depthHistogramMap = new float[maxDepth];

        if(_rawImage != null){
            _rawImage.texture = _depthMapTexture;
        }
    }

    void UpdateHistogram()
	{
		int i, numOfPoints = 0;
		
		Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        for (i = 0; i < _rawDepthMap.Length; i++) {
            // only calculate for valid depth
            if (_rawDepthMap[i] != 0) {
                depthHistogramMap[_rawDepthMap[i]]++;
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

    void UpdateDepthmapTexture()
    {
        int factor = 1;
        var xRes = _depthVideoStream.VideoMode.Resolution.Width;
        var yRes = _depthVideoStream.VideoMode.Resolution.Height;

		// flip the depthmap as we create the texture		
		int XScaled = xRes / factor;
        int YScaled = yRes / factor;

		int i = XScaled*YScaled-XScaled;
		int depthIndex = 0;
		for (int y = 0; y < YScaled; ++y, i-=XScaled)
		{
			for (int x = XScaled-1; x >= 0; --x, depthIndex += factor)
			{
				short pixel = _rawDepthMap[depthIndex];
               

                    
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
			depthIndex += (factor-1)*xRes; 
		}

		

		_depthMapTexture.SetPixels32(_depthMapPixels);
        _depthMapTexture.Apply();      
   }

    // EVENTS

    void OnNewDepthFrame(VideoStream vs){
        var frame = vs.ReadFrame();
        Marshal.Copy(frame.Data, _rawDepthMap, 0, _rawDepthMap.Length);
        
        
        Loom.QueueOnMainThread(() => {
            UpdateHistogram();
            UpdateDepthmapTexture();
            _depthMeshRenderer.UpdateMesh(_rawDepthMap);
        });        
    }


    void OnDeviceConnectionStateChanged(DeviceInfo deviceInfo){
        Debug.Log($"OnDeviceConnectionStateChanged: {deviceInfo.Name}, valid: {deviceInfo.IsValid}");
        
        if(deviceInfo.IsValid){
            ConnectDevice(deviceInfo);
        }

    }  

    void OnDestroy(){
        // if(_depthVideoStream != null){
        //     _depthVideoStream.Stop();
        // }

        // if(_device != null){
        //     _device.Close();
        // }

        // OpenNI.Shutdown();

        
    }
    
}
