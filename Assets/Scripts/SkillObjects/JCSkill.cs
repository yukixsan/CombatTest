using UnityEngine;

public class JCSkill : SkillObject
{
   [Header("Scan area")]
   [SerializeField] private Vector2 scanBoxSize = new Vector2(8f, 6f);
    [SerializeField] private Vector2 scanBoxOffset = Vector2.zero;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxScanLifetime = 0.2f;

    [Header ("Hit object")]
    [SerializeField] private GameObject jcHitPrefab;

    private float _scanTimer = 0f;
    private bool _hasHit = false;

    public override void Initialize(PlayerSkillData data, Transform player)
    {
        base.Initialize(data, player);
        _scanTimer = maxScanLifetime;
        _hasHit = false;

        // Use pool return 
        var returnComp = GetComponent<SkillObjectReturn>();
        if (returnComp != null)
            returnComp.Setup(data.skillPrefab, maxScanLifetime);
    }

    private void Update()
    {
        if(_hasHit) return;

        _scanTimer -= Time.deltaTime;
        if(_scanTimer <= 0f)
        {
            return;
        }
        ScanArea();
    }

    private void ScanArea()
    {
        Vector3 centerWorld = transform.position + new Vector3(scanBoxOffset.x * _facing, scanBoxOffset.y, 0f);
        Collider[]hits = Physics.OverlapBox(centerWorld, new Vector3(scanBoxSize.x * 0.5f, scanBoxSize.y * 0.5f, 0f), 
                         Quaternion.identity, enemyLayer);
        if(hits.Length <= 0) return;

        //Find nearest and grounded enemy
        Collider targetEnemy = null;
        float lowestY = float.MaxValue;
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            var hurtbox = col.GetComponent<EnemyHurtbox>();
            if (hurtbox == null) continue;

            float enemyY = col.transform.position.y;
            float dist = Vector3.Distance(transform.position, col.transform.position);

            if(enemyY < lowestY || (Mathf.Approximately(enemyY, lowestY) && dist < closestDist))
            {
                lowestY = enemyY;
                closestDist = dist;
                targetEnemy = col;
            }
        }
        if(targetEnemy != null)
        {
            ExecuteJCHit(targetEnemy.transform.position);
        }

    }

    private void ExecuteJCHit(Vector3 hitPosition)
    {
        // Implementation for executing JC hit
        _hasHit = true;
        //Spawn JC Hit effect
        GameObject jcHitObj = Instantiate(jcHitPrefab,hitPosition, Quaternion.identity);

        var hit = jcHitObj.GetComponent<JCHitObject>();
        if(hit != null)
        {
            hit.Initialize(payload, transform.localScale.x);
        }
        

    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(scanBoxOffset.x * _facing, scanBoxOffset.y, 0f);
        Gizmos.DrawWireCube(center, new Vector3(scanBoxSize.x, scanBoxSize.y, 1f));
    }

}
