using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;

// [RequireComponent(typeof (Collider2D))]
public class Creature : MonoBehaviour
{
    public float readOnlyEnergyChangeRate;
    // current stats
    public float energy;
    private Vector2 velocity;

    public CreatureStat stat;

    
    public float realEnergyGenRate {
        get
        {
            return GameManager.instance.energyAutoGenRate * (1 - (GameManager.instance.creatures.Count / GameManager.instance.maxSupportedLife));
        }
        private set{ }
    }
    public float energyConsumptionRate
    {
        get
        {
            return stat.size * Mathf.Pow(stat.speed, 2) + GameManager.instance.homeostasisConstant * Mathf.Pow(stat.size, 0.75f);
        }
    }
    private float lastDecisionTime;
    private float spawnTime;
    private float transformSize;
    Coroutine growCoroutine;
    public string color {get; private set;}

    [SerializeField] private bool showGizmos;

    [Header("Test Vars")]
    [SerializeField] private bool logNeighbors;

    public void Start()
    {
        // energy gen = clamp{maxEnergyAutoGenRate * (log(size/max_size)) / log(s_min/size_max), 0, maxEnergyAutoGenRate}
        transformSize = Mathf.Pow(stat.size, GameManager.instance.transformSizePower);
        color = GetComponent<SpriteRenderer>().color.ToHexString();

        spawnTime = Time.time;
        growCoroutine = StartCoroutine(Grow());
    }


  private void Update()
    {

        // Debug
        if (logNeighbors)
        {
            Debug.Log("found neighbors: " + DetectNearCreatures().Select(c => c.transform.name).ToArray().Length);
        }

        // Decide Direction to Move in
        if (lastDecisionTime + GameManager.instance.creatureDecisionRefreshTime < Time.time)
        {
            lastDecisionTime = Time.time;
            Creature[] neighbors = DetectNearCreatures();

            if (neighbors.Length == 0)
            {
                if (velocity == Vector2.zero)
                {
                    velocity =new Vector2(UnityEngine.Random.value, UnityEngine.Random.value).normalized * stat.speed;
                }
            }
            else
            {
                Vector2 dir = EncounterDecision(neighbors);
                velocity = dir * stat.speed;
            }
        }
        // Move Creature
        if (transform.position.x < 0 && velocity.x < 0 || transform.position.x > GameManager.instance.width && velocity.x > 0)
        {
            velocity.x *= -1;
        }
        if (transform.position.y < 0 && velocity.y < 0 || transform.position.y > GameManager.instance.height && velocity.y > 0)
        {
            velocity.y *= -1;
        }
        transform.Translate(velocity * Time.deltaTime);

        // passive energy calculation
        energy -= energyConsumptionRate * Time.deltaTime;
        energy += realEnergyGenRate * Time.deltaTime;
        readOnlyEnergyChangeRate = realEnergyGenRate - energyConsumptionRate;

        // Reproduction
        if (energy > stat.splitThresh)
        {
            GameManager.instance.SpawnOffspring(this);
        }  

        // kill creature if energy < 0:
        if (energy < 0)
        {
            KillSelf();
        }

    }

    private Creature[] DetectNearCreatures()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stat.detectRange);
        Collider2D[] creatures = hits.Where(x => x.tag.Equals("creature") && x.transform != transform).ToArray();

        return creatures.Select(c => c.GetComponent<Creature>()).ToArray();
        //stat.detectRange
    }

    public float DotWithConstant(float[] weights, float[] values)
    {
        float ret = weights[weights.Length - 1];

        if (weights.Length != values.Length + 1)
        {
            Debug.LogError("Dot Product called with incorrect Array Lengths");
        }
        for (int i = 0; i < values.Length; i++)
        {
            ret += weights[i] * values[i];
        }

        return ret;
    }

    private Vector2 CalculateResponseToOther(Creature other)
    {
        Vector2 dir = (other.transform.position - transform.position).normalized;
        float dst = Mathf.Min(Vector2.Distance(transform.position, other.transform.position), stat.detectRange);
        float closeness = Mathf.Max(stat.detectRange - dst, .1f * stat.detectRange) / stat.detectRange;

        // float dstWeightedVal = DotWithConstant(stat.encounterWeights.distanceWeights, new float[] {closeness});
        float speedWeightedVal = DotWithConstant(stat.encounterWeights.speedWeights, new float[] {other.stat.speed});
        float sizeWeightedVal = DotWithConstant(stat.encounterWeights.sizeWeights, new float[] {other.stat.size});

        Vector2 responseToC = (speedWeightedVal + sizeWeightedVal) * dir * closeness;

        return responseToC;
    }

    /// <summary>
    /// Calculates the direction that the creature will move towards
    /// </summary>
    /// <param name="others"></param>
    /// <returns></returns>
    public Vector2 EncounterDecision(Creature[] others)
    {
        Vector2 ret = Vector2.zero;
        foreach (Creature c in others)
        {
            ret += CalculateResponseToOther(c);
        }
        return ret.normalized;
    }

  private void OnCollisionEnter2D(Collision2D collision)
  {
    collision.gameObject.TryGetComponent(out Creature other);
    if (stat.size > other.stat.size * GameManager.instance.sizeThresholdToEat)
    {
        if (Time.time > spawnTime + GameManager.instance.spawnFeedingGracePeriod && Time.time > other.spawnTime + GameManager.instance.spawnFeedingGracePeriod / 3)
        {
            energy += other.stat.size * GameManager.instance.energyPerMassConversion + other.energy + GameManager.instance.eatBaseReward;
        }
        other.KillSelf();
    }
    else if (stat.size >= other.stat.size)
    {
        KillSelf();
        other.KillSelf();
    }
  }

    private void KillSelf()
    {
        GameManager.instance.creatures.Remove(this);
        StopCoroutine(growCoroutine);
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Shrink());
        //Destroy(gameObject);
    }

    IEnumerator Shrink()
    {
        float startTime = Time.time;
        float endTime = Time.time + GameManager.instance.growShrinkTime;
        while (Time.time < endTime)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(transformSize, 0, (Time.time - startTime) / (endTime - startTime));
            yield return new WaitForSeconds(1f);
        }
        Destroy(gameObject);
    }
    IEnumerator Grow()
    {
        float startTime = Time.time;
        float endTime = Time.time + GameManager.instance.growShrinkTime;
        while (Time.time < endTime)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(0, transformSize, (Time.time - startTime) / (endTime - startTime));
            yield return new WaitForSeconds(1f);
        }
        transform.localScale = Vector3.one * transformSize;
    }
    

  void OnDrawGizmos()
  {
    if (GameManager.instance.showGizmos || showGizmos)
        {
            Gizmos.color = UnityEngine.Color.white;
            Gizmos.DrawWireSphere(transform.position, stat.detectRange);

            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawLine((Vector2) transform.position, (Vector2) transform.position + 3 * EncounterDecision(DetectNearCreatures()));
        }
  }
}

[Serializable]
public struct CreatureStat
{
    public float speed; // balanced by energy consumption

    public float detectRange;
    public float size; // balanced by energy consumption
    public float splitThresh;
    public float spawnDist; // balanced by flat energy cost on reproduction
    public EncounterDecisionWeights encounterWeights;

    public CreatureStat(
        float speed,
        float detectRange,
        float size,
        float split_thresh,
        float spawn_dist,
        EncounterDecisionWeights encounterWeights
    ) {
        this.speed = (float) Mathf.Max(0, speed);
        this.detectRange = (float) Mathf.Max(0.01f, detectRange);
        this.size = (float) Mathf.Max(GameManager.instance.minSizeLimit, size);
        this.splitThresh = split_thresh;
        this.spawnDist = spawn_dist;
        this.encounterWeights = encounterWeights;
    }

    public CreatureStat Mutate(float mutationPercent)
    {
        return new CreatureStat(
            speed * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f)),
            detectRange * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f)),
            size * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f)),
            splitThresh * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f)),
            spawnDist * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f)),
            encounterWeights.Mutate(mutationPercent)
        );
    }

    public override string ToString()
    {
        return String.Join(",", new string[] {speed.ToString(), detectRange.ToString(), size.ToString(), splitThresh.ToString(), spawnDist.ToString(), encounterWeights.ToString()});
    }
}

[Serializable]
public struct EncounterDecisionWeights
{
    // represented by vector2 [otherValue, constant]
    public float[] speedWeights;
    public float[] sizeWeights;

    public EncounterDecisionWeights(float[] speedWeights, float[] sizeWeights)
    {
        this.speedWeights = speedWeights;
        this.sizeWeights = sizeWeights;
    }
    public EncounterDecisionWeights Mutate(float mutationPercent)
    {
        return new EncounterDecisionWeights(
            speedWeights.Select(x => Mathf.Clamp(x + mutationPercent * UnityEngine.Random.Range(-1f, 1f),-1, 1)).ToArray(),
            sizeWeights.Select(x => Mathf.Clamp(x + mutationPercent * UnityEngine.Random.Range(-1f, 1f), -1, 1)).ToArray()
        );
    }

    /// <summary>
    /// returns in form: speedCoef, speedConst, sizeCoef, sizeConst
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Join(",", speedWeights) + "," + String.Join(",", sizeWeights);
    }
}