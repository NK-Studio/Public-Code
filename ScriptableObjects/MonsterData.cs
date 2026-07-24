using UnityEngine;

namespace BounceHeroes.Data
{
    /// <summary>
    /// Defines monster stats, footprint, and visual source data.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Monster", menuName = "BounceHeroes/Monster")]
    public class MonsterData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Sprite blockSprite;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float bodyOffsetY = 0.29f;

        [Header("Base HP")]
        [SerializeField] private int baseHp = 25;

        [Header("Footprint")]
        [SerializeField] private int footprintWidth = 1;
        [SerializeField] private int footprintHeight = 1;

        /// <summary>Display name shown for this monster.</summary>
        public string DisplayName => displayName;

        /// <summary>Legacy single-frame body sprite.</summary>
        public Sprite Sprite => sprite;

        /// <summary>Block sprite used when no prefab-based visual is assigned.</summary>
        public Sprite BlockSprite => blockSprite;

        /// <summary>Prefab-composed monster GFX. Falls back to legacy sprites when null.</summary>
        public GameObject VisualPrefab => visualPrefab;

        /// <summary>Base HP before game-balance multipliers are applied.</summary>
        public int BaseHp => baseHp;

        /// <summary>Damage dealt to the player when the monster reaches the bottom.</summary>
        public int AttackDamage => attackDamage;

        /// <summary>Local Y offset of the body sprite above the block.</summary>
        public float BodyOffsetY => bodyOffsetY;

        /// <summary>Number of grid columns occupied by this monster.</summary>
        public int FootprintWidth => footprintWidth;

        /// <summary>Number of grid rows occupied by this monster.</summary>
        public int FootprintHeight => footprintHeight;
    }
}
