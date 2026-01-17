using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
  public static GameManager instance;
  public Creature creaturePrefab;

  [Header("global settings")]
  public float creatureDecisionRefreshTime;
  public float width, height;
  [Range (0.01f, 0.10f)]
  public float mutationRange;

  [Header("World Rules")]
  public float energyAutoGenRate = 10;
  public float energyPerSpawnDst = 100;

  [Header("Starting Population")]
  public int startingPopulationSize = 5;
  public float originalCreatureStartingEnergy = 1000;
  public CreatureStat minStat;
  public CreatureStat maxStat;

  // Logger Information
  [HideInInspector]
  public List<Creature> creatures;

  private void Awake()
  {
    if (instance != null) Destroy(this);
    else instance = this;

    SpawnBatch(startingPopulationSize);
  }
  public Creature SpawnCreature(CreatureStat stat, Vector2 position, float startingEnergy)
  {
    Creature c = Instantiate(creaturePrefab, position, Quaternion.identity);
    c.stat = stat;
    c.energy = startingEnergy;
    creatures.Add(c);
    return c;
  }

  public Creature SpawnOffspring(Creature parent)
  {
    CreatureStat childStat = parent.stat.Mutate(mutationRange);
    Vector2 spawnPosition;
    do
    {
      float degree = UnityEngine.Random.Range(0, Mathf.PI * 2);
      Vector2 dir = new Vector2(Mathf.Cos(degree), Mathf.Sin(degree));
      spawnPosition = (Vector2) parent.transform.position + dir * parent.stat.spawnDist;
    }
    while (!(0 < spawnPosition.x && spawnPosition.x < width && 0 < spawnPosition.y && spawnPosition.y < height));

    float startingEnergy = (parent.energy / 2) - energyPerSpawnDst * parent.stat.spawnDist;
    parent.energy = startingEnergy;
    
    return SpawnCreature(childStat, spawnPosition, startingEnergy);
  }

  private void SpawnBatch(int spawnCount)
  {
    for (int i = 0; i < spawnCount; i++)
    {
      CreatureStat stat = new CreatureStat(
        Random.Range(minStat.speed, maxStat.speed),
        Random.Range(minStat.detectRange, maxStat.detectRange),
        Random.Range(minStat.size, maxStat.size),
        Random.Range(minStat.splitThresh, maxStat.splitThresh),
        Random.Range(minStat.spawnDist, maxStat.spawnDist),
        new EncounterDecisionWeights(
          new float[] {
            Random.Range(minStat.encounterWeights.speedWeights[0], maxStat.encounterWeights.speedWeights[0]),
            Random.Range(minStat.encounterWeights.speedWeights[1], maxStat.encounterWeights.speedWeights[1])
          },
          new float [] {
            Random.Range(minStat.encounterWeights.sizeWeights[0], maxStat.encounterWeights.sizeWeights[0]),
            Random.Range(minStat.encounterWeights.sizeWeights[1], maxStat.encounterWeights.sizeWeights[1])
          }
        )
      );

      Vector2 position = new Vector2(Random.Range(0, width), Random.Range(0, height));
      SpawnCreature(stat, position, originalCreatureStartingEnergy);
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.DrawWireCube(new Vector2(width /2, height / 2), new Vector2(width, height  ) );
  }
}
