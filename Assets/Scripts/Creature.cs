using System;
using System.Collections;
using System.Collections.Generic;
// using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof (Collider2D))]
public class Creature : MonoBehaviour
{
    public CreatureStat stat;

    // current stats
    public float energy;
    private Vector2 velocity;
    private float lastDecisionTime;


    [Header("Test Vars")]
    [SerializeField] private bool logNeighbors;
    [SerializeField] private bool showGizmos;
    
    
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
                // wander
            }
            else
            {
                Vector2 dir = EncounterDecision(neighbors);
                velocity = dir * stat.speed;
            }
        }
        // Move Creature
        transform.Translate(velocity * Time.deltaTime);

        // passive energy calculation
        energy -= stat.size * (stat.speed * stat.speed) * Time.deltaTime;
        energy += GameManager.instance.energyAutoGenRate * Time.deltaTime;        
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
        energy += other.stat.size + other.energy;
        Destroy(other.gameObject);
    }
    else if (stat.size > other.stat.size)
    {
        Destroy(other.gameObject);
        Destroy(gameObject);
    }
  }

  void OnDrawGizmos()
  {
    if (showGizmos)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, stat.detectRange);

            Gizmos.color = Color.red;
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
        this.speed = speed;
        this.detectRange = detectRange;
        this.size = size;
        this.splitThresh = split_thresh;
        this.spawnDist = spawn_dist;
        this.encounterWeights = encounterWeights;
    }
}

[Serializable]
public struct EncounterDecisionWeights
{
    // represented by vector2 [otherValue, constant]
    public float[] speedWeights;
    public float[] sizeWeights;

    // represented by Vector2 [distance, constant]
    public float[] distanceWeights;

    public EncounterDecisionWeights(float[] speedWeights, float[] sizeWeights, float[] distanceWeights)
    {
        this.speedWeights = speedWeights;
        this.sizeWeights = sizeWeights;
        this.distanceWeights = distanceWeights;
    }
}