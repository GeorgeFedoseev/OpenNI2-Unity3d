using System.Collections.Generic;
using UnityEngine;

public sealed class PointCloudData : ScriptableObject
{
    /// Byte size of the point element.
    public const int elementSize = sizeof(float) * 4;

    private object _pointDataLock = new object();

    /// Number of points.
    public int pointCount {
        get { return _pointData.Length; }
    }

    /// Get access to the compute buffer that contains the point cloud.
    public ComputeBuffer computeBuffer {
        get {
            if (_pointBuffer == null)
            {
                _pointBuffer = new ComputeBuffer(pointCount, elementSize);                
            }

            lock(_pointDataLock){
                _pointBuffer.SetData(_pointData);
            }
            
            return _pointBuffer;
        }
    }
   
    ComputeBuffer _pointBuffer;

    void OnDisable()
    {
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }
    }


    [System.Serializable]
    public struct Point
    {
        public Vector3 position;
        public uint color;
    }

    [SerializeField] public Point[] _pointData;

    static uint EncodeColor(Color c)
    {
        const float kMaxBrightness = 16;

        var y = Mathf.Max(Mathf.Max(c.r, c.g), c.b);
        y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

        var rgb = new Vector3(c.r, c.g, c.b);
        rgb *= 255 * 255 / (y * kMaxBrightness);

        return ((uint)rgb.x      ) |
                ((uint)rgb.y <<  8) |
                ((uint)rgb.z << 16) |
                ((uint)y     << 24);
    }

    public void Initialize(List<Vector3> positions/*, List<Color32> colors*/)
    {

        lock(_pointDataLock){
            _pointData = new Point[positions.Count];
            for (var i = 0; i < _pointData.Length; i++)
            {
                _pointData[i] = new Point {
                    position = positions[i],
                    // color = EncodeColor(colors[i])
                    color = EncodeColor(Color.green)
                };
            }
        }
        
    }
}