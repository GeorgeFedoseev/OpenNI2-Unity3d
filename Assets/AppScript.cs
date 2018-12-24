using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenNIWrapper;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using System;

public class AppScript : MonoBehaviour
{

    private Device _currentDevice;
    private VideoStream _depthVideoStream;   

    private int _depthMapWidth, _depthMapHeight;
    private float _hFOV, _vFOV;

    // depth frame data
    private short[] _rawDepthMap;    

    // Texture2D rendering
    [SerializeField]
    private DepthTextureRendererScript _textureRenderer;

    // Mesh rendering
    [SerializeField]
    private DepthMeshRenderer _depthMeshRenderer;

    // PointCloud rendering
    private List<Vector3> _pointPositions = null;    
    private PointCloudData _pointCloudData = null;
    [SerializeField]
    private PointCloudRenderer _pointCloudRenderer;


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

        _hFOV = _depthVideoStream.HorizontalFieldOfView;
        _vFOV = _depthVideoStream.VerticalFieldOfView;

        _depthMapWidth = _depthVideoStream.VideoMode.Resolution.Width;
        _depthMapHeight = _depthVideoStream.VideoMode.Resolution.Height;

        Debug.Log($"H_FOV: {_hFOV/Math.PI*180}");
        Debug.Log($"V_FOV: {_vFOV/Math.PI*180}");

        
        Debug.Log($"depth resolution: {_depthMapWidth}x{_depthMapHeight}");

        // init raw depth data
        _rawDepthMap = new short[(int)(_depthMapWidth * _depthMapHeight)];


        // Mesh rendering
        _depthMeshRenderer.Init(
            _depthMapWidth,
            _depthMapHeight,
            _depthVideoStream.HorizontalFieldOfView,
            _depthVideoStream.VerticalFieldOfView
        );

        // PointCloud
        _pointPositions = new List<Vector3>(_depthMapWidth * _depthMapHeight);
        _pointCloudData = new PointCloudData();
        _pointCloudData.Initialize(_pointPositions);
        _pointCloudRenderer.sourceData = _pointCloudData;

        
        // Texture
        _textureRenderer.Init(_depthMapWidth, _depthMapHeight, _depthVideoStream.MaxPixelValue);
        
    }

    private void CalculatePointPositions(){

        for (int y = 0; y < _depthMapHeight; y++)
        {
            for (int x = 0; x < _depthMapWidth; x++)
            {
                int index = (y * _depthMapWidth) + x;

                
            }
        }
    }

    

    // EVENTS

    void OnNewDepthFrame(VideoStream vs){
        var frame = vs.ReadFrame();
        Marshal.Copy(frame.Data, _rawDepthMap, 0, _rawDepthMap.Length);
        
        
        Loom.QueueOnMainThread(() => {            
            _depthMeshRenderer.UpdateMesh(_rawDepthMap);
            _textureRenderer.UpdateTexture(_rawDepthMap);
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
