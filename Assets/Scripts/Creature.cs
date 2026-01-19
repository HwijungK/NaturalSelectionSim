using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof (Collider2D))]
public class Creature : MonoBehaviour
{
    // current stats
    public float energy;
    private Vector2 velocity;

    public CreatureStat stat;
    public float energyGenRate {get; private set;}
    public float energyConsumptionRate
    {
        get
        {
            return stat.size * Mathf.Pow(stat.speed, 2);
        }
    }
    private float lastDecisionTime;

    public string color {get; private set;}

    [Header("Test Vars")]
    [SerializeField] private bool logNeighbors;

    public void Start()
    {
        // energy gen = clamp{maxEnergyAutoGenRate * (log(size/max_size)) / log(s_min/size_max), 0, maxEnergyAutoGenRate}
        energyGenRate = Mathf.Clamp(
            GameManager.instance.maxEnergyAutoGenRate * (Mathf.Log(stat.size / GameManager.instance.energyGenMaxSizeLim) / Mathf.Log(GameManager.instance.energyGenMinSizeLim/GameManager.instance.energyGenMaxSizeLim)),
            0,
            GameManager.instance.maxEnergyAutoGenRate
        );
        float transform_size = Mathf.Log(stat.size / GameManager.instance.energyGenMinSizeLim, 2) + GameManager.instance.energyGenMinSizeLim;
        transform.localScale = Vector3.one * transform_size;
        color = GetComponent<SpriteRenderer>().color.ToHexString();
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
        energy -= stat.size * Mathf.Pow(velocity.magnitude, 2) * Time.deltaTime;
        energy += energyGenRate * Time.deltaTime;

        // Reproduction
        if (energy > stat.splitThresh)
        {
            GameManager.instance.SpawnOffspring(this);
        }  

        // kill creature if energy < 0:
        // create a corpse?
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
        float dst = Vector2.Distance(transform.position, other.transform.position);
        float closeness = (stat.detectRange - dst) / stat.detectRange;

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
    if (stat.size > other.stat.size * 1.3)
    {
        energy += other.stat.size * GameManager.instance.energyPerMassConversion + other.energy;
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
        Destroy(gameObject);
    }

  void OnDrawGizmos()
  {
    if (GameManager.instance.showGizmos)
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
        this.size = size = (float) Mathf.Max(.5f, size);
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
            speedWeights.Select(x => x * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f))).ToArray(),
            sizeWeights.Select(x => x  * (1 + mutationPercent * UnityEngine.Random.Range(-1f, 1f))).ToArray()
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