using System.Collections.Generic;
using UnityEngine;

namespace BounceHeroes.Gameplay
{
    /// <summary>
    /// 조준 중 볼의 예상 궤도를 계산해 표시합니다.
    /// 벽은 반사면으로 취급해 궤도를 계속 꺾어 그리고, 몬스터는 반사면이 아니라
    /// "여기서 처음 맞는다"는 종점으로 취급해 그 지점에서 궤도를 멈춥니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private LayerMask wallMask;
        [SerializeField] private LayerMask stopMask;
        [SerializeField] private int maxBounces = 2;
        [SerializeField] private float maxDistance = 16f;
        [SerializeField] private float castRadius = 0.16f;

        [Tooltip("궤도 끝점에 배치되는 빈 오브젝트입니다. 이펙트 등을 붙일 수 있습니다.")]
        [SerializeField] private Transform endpointMarker;
        [Tooltip("궤도 끝점 마커의 초당 회전 속도 (도 단위)")]
        [SerializeField] private float endpointRotationSpeed = 150f;
        [Header("Dashed Line")]
        [SerializeField, Min(0.01f)] private float textureTiling = 2.2f;
        [SerializeField] private float flowSpeed = 1.2f;

        private readonly List<Vector3> _points = new List<Vector3>();
        private LineRenderer _line;
        private Material _lineMaterial;
        private float _flowOffset;
        private float _endpointRotationAngle;
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int FlowOffsetId = Shader.PropertyToID("_FlowOffset");

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _lineMaterial = _line.material;
            _line.textureMode = LineTextureMode.Tile;
            _line.enabled = false;
            ApplyDashedLineSettings();

            if (endpointMarker != null)
                endpointMarker.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_lineMaterial == null)
                return;

            _flowOffset = Mathf.Repeat(_flowOffset - Time.deltaTime * flowSpeed, 1f);
            _lineMaterial.SetFloat(FlowOffsetId, _flowOffset);
            
            if (endpointMarker != null)
            {
                _endpointRotationAngle = Mathf.Repeat(_endpointRotationAngle + Time.deltaTime * endpointRotationSpeed, 360f);
                endpointMarker.rotation = Quaternion.Euler(0f, 0f, _endpointRotationAngle);
            }
        }

        private void OnValidate()
        {
            textureTiling = Mathf.Max(0.01f, textureTiling);

            if (_line == null)
                _line = GetComponent<LineRenderer>();

            if (_line != null)
            {
                Material targetMaterial = Application.isPlaying ? _lineMaterial : _line.sharedMaterial;
                ApplyDashedLineSettings(targetMaterial);
            }
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }

        /// <summary>
        /// 지정한 시작점과 방향으로 예상 궤도를 계산해 표시합니다.
        /// </summary>
        /// <param name="origin">발사 시작점</param>
        /// <param name="direction">발사 방향(정규화 필요 없음)</param>
        public void Show(Vector2 origin, Vector2 direction)
        {
            _points.Clear();
            _points.Add(origin);

            Vector2 position = origin;
            Vector2 currentDirection = direction.normalized;
            float remaining = maxDistance;
            LayerMask combinedMask = wallMask | stopMask;

            for (int bounce = 0; bounce <= maxBounces && remaining > 0f; bounce++)
            {
                RaycastHit2D hit = Physics2D.CircleCast(
                    position + currentDirection * 0.05f, castRadius, currentDirection, remaining, combinedMask);

                if (hit.collider == null)
                {
                    _points.Add(position + currentDirection * remaining);
                    break;
                }

                _points.Add(hit.centroid);

                bool isWall = IsInMask(hit.collider.gameObject.layer, wallMask);

                if (!isWall)
                    break;

                remaining -= hit.distance;
                currentDirection = Vector2.Reflect(currentDirection, hit.normal);
                position = hit.centroid;
            }

            _line.positionCount = _points.Count;

            for (int i = 0; i < _points.Count; i++)
            {
                _line.SetPosition(i, _points[i]);
            }

            ApplyDashedLineSettings();
            _line.enabled = true;

            if (endpointMarker != null && _points.Count > 0)
            {
                endpointMarker.position = _points[_points.Count - 1];
                endpointMarker.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 궤도 표시를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            _line.enabled = false;

            if (endpointMarker != null)
                endpointMarker.gameObject.SetActive(false);
        }

        private static bool IsInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void ApplyDashedLineSettings()
        {
            ApplyDashedLineSettings(_lineMaterial);
        }

        private void ApplyDashedLineSettings(Material targetMaterial)
        {
            if (_line != null)
            {
                _line.textureMode = LineTextureMode.Tile;
                _line.textureScale = new Vector2(textureTiling, 1f);
            }

            if (targetMaterial != null)
            {
                targetMaterial.SetTextureScale(MainTexId, Vector2.one);
                targetMaterial.SetFloat(FlowOffsetId, _flowOffset);
            }
        }
    }
}
