using UnityEngine;

namespace SkillSystem
{
    public class Skill : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5.0f;
        [SerializeField] public ParticleSystem skillEffect;

        public float LifetimeSeconds => lifetime;
        protected float Lifetime => lifetime;
        protected void DestroyAfterLifetime(GameObject go)
        {
            if (go) Destroy(go, lifetime);
        }

        private Vector2Int targetGridPosition;
        private SkillCombination skillType;

        public virtual void Initialize(Vector2Int gridPos, SkillCombination type, Transform caster)
        {
            targetGridPosition = gridPos;
            skillType = type;

            Destroy(gameObject, lifetime);

            ExecuteSkillEffect(gridPos, caster);
            if (skillEffect != null) skillEffect.Play();
        }

        public virtual void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform) { }

        protected Vector2Int GetTargetGridPosition() => targetGridPosition;
        protected SkillCombination GetSkillType() => skillType;
    }
}
