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


    [SerializeField]
    Transform _testQuad;


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
        _pointPositions = new List<Vector3>();
        for(int i = 0; i < _depthMapWidth * _depthMapHeight; i++){
            _pointPositions.Add(new Vector3());
        }

        _pointCloudData = ScriptableObject.CreateInstance<PointCloudData>();
        _pointCloudData.Initialize(_pointPositions);
        _pointCloudRenderer.sourceData = _pointCloudData;

        
        // Texture
        _textureRenderer.Init(_depthMapWidth, _depthMapHeight, _depthVideoStream.MaxPixelValue);
        
    }

    private void CalculatePointPositions(){
        // print("CalculatePointPositions");

        var px_to_deg = new Vector2(
            _hFOV/_depthMapWidth,
            _vFOV/_depthMapHeight
        );

                 

        for (int y = 0; y < _depthMapHeight; y++)
        {
            for (int x = 0; x < _depthMapWidth; x++)
            {
                int index = (y * _depthMapWidth) + x;

                // var pos = _pointPositions[index];
                var distanceZ = _rawDepthMap[index] / 1000f;

                var x_from_center = x - _depthMapWidth/2;
                var angleX = x_from_center*px_to_deg.x;

                var y_from_center = y - _depthMapHeight/2;
                y_from_center = -y_from_center;
                var angleY = y_from_center*px_to_deg.y;

                _pointPositions[index] = new Vector3(
                    distanceZ * Mathf.Sin(angleX),
                    distanceZ * Mathf.Sin(angleY),
                    distanceZ
                );
                

        //        print(_pointPositions[index]);
            }
        }

        _pointCloudData.Initialize(_pointPositions);
    }

    private void CalculateDistancesToQuad(){
        // print("CalculateDistancesToQuad");
        var plane = new Plane(-_testQuad.transform.forward, _testQuad.transform.position);

        int pointProjectedOnPlaneCount = 0;
        foreach(var p in _pointPositions.ToArray()){

            var point_on_plane = plane.ClosestPointOnPlane(p);
            

            var distance_to_plane = (p - point_on_plane).magnitude;
            

            var distance_on_plane = (point_on_plane-_testQuad.transform.position).magnitude;
            
            if(distance_on_plane < _testQuad.localScale.x/2 && distance_to_plane < 0.01f){                
                _pointToDraw = point_on_plane;
                pointProjectedOnPlaneCount++;
            }
        }

        if(pointProjectedOnPlaneCount > 0){
            print($"Points projected on plane: {pointProjectedOnPlaneCount}");
        }
        
    }

    Vector3 _pointToDraw;
    void OnDrawGizmos(){
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_pointToDraw, 0.1f);
    }

    

    // EVENTS

    void OnNewDepthFrame(VideoStream vs){
        if(this == null){
            return;
        }

        // print("OnNewDepthFrame");

        var frame = vs.ReadFrame();
        Marshal.Copy(frame.Data, _rawDepthMap, 0, _rawDepthMap.Length);
        

        CalculatePointPositions();

        
        
        Loom.QueueOnMainThread(() => {            
            CalculateDistancesToQuad();

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

        // Loom.RunAsync(() => {
            // if(_depthVideoStream != null){
            //     // _depthVideoStream.OnNewFrame -= OnNewDepthFrame;
            //     _depthVideoStream.Stop();
            // }

            // if(_currentDevice != null){
            //     _currentDevice.Close();
            // }

        // });
        

        
        
    }
    
}
