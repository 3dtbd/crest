﻿using UnityEngine;

namespace Crest
{
    public class OceanFX : MonoBehaviour
    {
        [Range(0, 63)]
        public int _samplesPerFrame = 32;

        public ParticleSystem _particlePrefab;

        public float _emitThresh = 0.7f;
        public float _minYVel = 1f;
        public bool _showPoints = false;

        Camera _cam;

        private void Start()
        {
            if(OceanRenderer.Instance.Viewpoint != null)
            {
                _cam = OceanRenderer.Instance.Viewpoint.GetComponent<Camera>();
            }

            if (_cam == null)
            {
                _cam = Camera.main;
            }
        }

        void Update()
        {
            var seaLevel = OceanRenderer.Instance.SeaLevel;

            for (int i = 0; i < _samplesPerFrame; i++)
            {
                // this tests on screen positions undisplaced positions. it might be interesting to look a bit outside frustum.
                var randomPos = new Vector3(Random.value * Screen.width, Random.value * Screen.height, 0f);
                var ray = Camera.main.ScreenPointToRay(randomPos);
                // if close to or above horizon, just skip this sample. another option would be to negate the y component of the direction
                if (ray.direction.y > -0.01f)
                    continue;

                var queryPos = ray.origin + ray.direction * (seaLevel - ray.origin.y) / ray.direction.y;
                Vector3 disp, vel;
                //_gerstner.SampleDisplacement(ref queryPos, out disp, 0f);
                bool dispValid, velValid;
                OceanRenderer.Instance.CollisionProvider.SampleDisplacementVel(ref queryPos, out disp, out dispValid, out vel, out velValid, 0f);
                if (_showPoints)
                {
                    Debug.DrawLine(queryPos, queryPos + disp);
                }

                float ss = 0.5f;

                var queryPos_x = queryPos + Vector3.right * ss;
                Vector3 disp_x;
                OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref queryPos_x, out disp_x);

                var queryPos_z = queryPos + Vector3.forward * ss;
                Vector3 disp_z;
                OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref queryPos_z, out disp_z);

                disp_x += Vector3.right * ss;
                disp_z += Vector3.forward * ss;

                float dux = disp_x.x - disp.x;
                float duy = disp_x.z - disp.z;
                float duz = disp_z.x - disp.x;
                float duw = disp_z.z - disp.z;
                // The determinant of the displacement Jacobian is a good measure for turbulence:
                // > 1: Stretch
                // < 1: Squash
                // < 0: Overlap
                float det = (dux * duw - duy * duz) / (ss * ss);
                det = Mathf.InverseLerp(1.6f, 0f, det);

                if (det >= _emitThresh && vel.y >= _minYVel)
                {
                    var ps = Instantiate(_particlePrefab.transform);
                    ps.position = queryPos + disp;
                    ps.LookAt(ps.position + Vector3.Lerp(Vector3.up, vel, Random.value));
                    ps.GetComponent<TrackOceanSurface>()._basePosition = queryPos;
                }
            }
        }
    }
}
