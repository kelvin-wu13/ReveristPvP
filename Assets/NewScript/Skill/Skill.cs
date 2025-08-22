using UnityEngine;

namespace SkillSystem
{
    public class Skill : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5.0f;
        [SerializeField] public ParticleSystem skillEffect;
        
        private Vector2Int targetGridPosition;
        private SkillCombination skillType;
        
        public virtual void Initialize(Vector2Int gridPos, SkillCombination type, Transform caster)
        {
            targetGridPosition = gridPos;
            skillType = type;
            
            Destroy(gameObject, lifetime);
            
            ExecuteSkillEffect(gridPos, caster);

            if (skillEffect != null)
            {
                skillEffect.Play();
            }
        }

        public virtual void ExecuteSkillEffect(Vector2Int targetPosition, Transform casterTransform)
        {

        }
        //protected virtual void OnEnemyHit(Enemy hitEnemy)
        //{
        //    if (hitEnemy != null)
        //    {
        //        SkillHitDetector.ReportSkillHit(skillType, hitEnemy);
        //    }
        //}
        //protected virtual void DealDamageToEnemy(Enemy enemy, int damage)
        //{
        //    if (enemy != null)
        //    {
        //        enemy.TakeDamage(damage);
        //        OnEnemyHit(enemy);
        //    }
        //}
        protected Vector2Int GetTargetGridPosition() => targetGridPosition;
        protected SkillCombination GetSkillType() => skillType;
    }
}