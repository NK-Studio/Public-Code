using UnityEngine;

namespace BounceHeroes.Data
{
    [CreateAssetMenu(fileName = "SO_BallVisual", menuName = "BounceHeroes/Ball Visual Profile")]
    public sealed class BallVisualProfile : ScriptableObject
    {
        [SerializeField] private Sprite sprite;

        [Header("Trail")]
        [SerializeField] private Color trailHeadColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color trailTailColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField, Min(0.01f)] private float trailTime = 0.28f;
        [SerializeField, Min(0.01f)] private float trailStartWidth = 0.18f;
        [SerializeField, Min(0f)] private float trailEndWidth = 0.02f;

        [Header("Ambient Particle (optional tint)")]
        [SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.65f);

        [Header("Hit Burst")]
        [SerializeField, Min(0f)] private float hitBurstCount = 10f;

        [Header("Motion")]
        [SerializeField, Min(1f)] private float pulseScale = 1.08f;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.16f;

        public Sprite Sprite => sprite;
        public Color TrailHeadColor => trailHeadColor;
        public Color TrailTailColor => trailTailColor;
        public float TrailTime => trailTime;
        public float TrailStartWidth => trailStartWidth;
        public float TrailEndWidth => trailEndWidth;
        public Color ParticleColor => particleColor;
        public int HitBurstCount => Mathf.Max(0, Mathf.RoundToInt(hitBurstCount));
        public float PulseScale => pulseScale;
        public float PulseDuration => pulseDuration;
    }
}
