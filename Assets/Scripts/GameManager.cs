using System.Collections.Generic;
using JetBrains.Annotations;
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

  [Range(0.01f, 50)]
  public float timeScale = 1f;

  [Header("World Rules")]
  // energy gen = clamp{maxEnergyAutoGenRate * (log(size/max_size)) / log(s_min/size_max), 0, maxEnergyAutoGenRate}
  public float maxEnergyAutoGenRate = 10;
  public float energyGenMinSizeLim = 0.5f;
  public float energyGenMaxSizeLim = 8;
  public float energyPerSpawnDst = 100;

  [Header("Starting Population")]
  public int startingPopulationSize = 5;
  public float originalCreatureStartingEnergy = 1000;
  public CreatureStat minStat;
  public CreatureStat maxStat;

  [Header("Gizmos")]
  public bool showGizmos = true;

  // Logger Information
  //[HideInInspector]
  public List<Creature> creatures;

  private void Awake()
  {
    Vector2 test = new Vector2(1,1);
    test.x *= -1;
    print("TEST: " + test.x);
    if (instance != null) Destroy(this);
    else instance = this;

    SpawnBatch(startingPopulationSize);
  }
  void Update()
  {
    Time.timeScale = timeScale;
  }
  public Creature SpawnCreature(CreatureStat stat, Vector2 position, float startingEnergy)
  {
    Creature c = Instantiate(creaturePrefab, position, Quaternion.identity);
    c.stat = stat;
    c.energy = startingEnergy;
    creatures.Add(c);
    return c;
  }
  public Creature SpawnCreature(CreatureStat stat, Vector2 position, float startingEnergy, Color color)
  {
    Creature c = Instantiate(creaturePrefab, position, Quaternion.identity);
    c.stat = stat;
    c.energy = startingEnergy;
    c.GetComponent<SpriteRenderer>().color = color;
    creatures.Add(c);
    return c;
  }

  public Creature SpawnOffspring(Creature parent)
  {
    CreatureStat childStat = parent.stat.Mutate(mutationRange);
    Vector2 spawnPosition;

    Color parentColor = parent.GetComponent<SpriteRenderer>().color;
    float r = Mathf.Clamp(parentColor.r * (1 + Random.Range(-1f, 1f) * mutationRange), 0, 255);
    float g = Mathf.Clamp(parentColor.g * (1 + Random.Range(-1f, 1f) * mutationRange), 0, 255);
    float b = Mathf.Clamp(parentColor.b * (1 + Random.Range(-1f, 1f) * mutationRange), 0, 255);
    Color childColor = new Color(r, g, b);

    int _attemptsToSpawn = 0;

    do
    {
      _attemptsToSpawn ++;
      float degree = UnityEngine.Random.Range(0, Mathf.PI * 2);
      Vector2 dir = new Vector2(Mathf.Cos(degree), Mathf.Sin(degree));
      spawnPosition = (Vector2) parent.transform.position + dir * parent.stat.spawnDist;
    }
    while (!(0 < spawnPosition.x && spawnPosition.x < width && 0 < spawnPosition.y && spawnPosition.y < height) && _attemptsToSpawn < 500);
    if (_attemptsToSpawn >= 500)
    {
      Debug.LogError("Parent is out of bounds as cannot spawn Offsprint");
    }

    float startingEnergy = (parent.energy / 2) - energyPerSpawnDst * parent.stat.spawnDist;
    parent.energy = startingEnergy;
    
    return SpawnCreature(childStat, spawnPosition, startingEnergy, childColor);
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

      Color color = new(Random.Range(0f,1),Random.Range(0f,1),Random.Range(0f,1));
      Vector2 position = new Vector2(Random.Range(0, width), Random.Range(0, height));
      SpawnCreature(stat, position, originalCreatureStartingEnergy, color);
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.DrawWireCube(new Vector2(width /2, height / 2), new Vector2(width, height  ) );
  }
}
