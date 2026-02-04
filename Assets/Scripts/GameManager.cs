using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore;

public class GameManager : MonoBehaviour
{
  public static GameManager instance;
  public Creature creaturePrefab;

  [Header("global settings")]
  public float creatureDecisionRefreshTime;
  public float width, height;
  [Range (0.01f, 0.20f)]
  public float mutationRange;
  [Tooltip("Increase or decrease the mutation of the color of creatures")]
  public float colorMutationMultiplier = 1.5f;

  [Range(0.01f, 99)]
  public float timeScale = 1f;

  [Header("World Rules")]
  public float energyAutoGenRate = 2;
  public float homeostasisConstant = .7f;
  public int maxSupportedLife = 5000;
  public float minSizeLimit = 0.5f;
  [Range(0, 2)]
  public float transformSizePower = .8f;
  public float energyPerSpawnDst = 100;
  public float energyPerMassConversion = 100;
  public float spawnFeedingGracePeriod = .4f; // prevents a creature from spawning and instantly feeding on the creatures it spawns on.
  public float sizeThresholdToEat = 1.1f;
  public float eatBaseReward = 200f;
  public float growShrinkTime = 5f;

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
    Color.RGBToHSV(parentColor, out float ph, out float ps, out float pv);
    float h = Mathf.Clamp(ph + (1 * Random.Range(-1f, 1f) * colorMutationMultiplier * mutationRange), 0, 1);
    float s = Mathf.Clamp(ps + (1 * Random.Range(-1f, 1f) * colorMutationMultiplier * mutationRange),.4f, 1);
    float v = Mathf.Clamp(pv + (1 * Random.Range(-1f, 1f) * colorMutationMultiplier * mutationRange),.6f, 1);


    // float r = Mathf.Clamp(parentColor.r + (parentColor.r * Random.Range(-1f, 1f) * mutationRange * colorMutationMultiplier), 0, 255);
    // float g = Mathf.Clamp(parentColor.g + (parentColor.g * Random.Range(-1f, 1f) * mutationRange * colorMutationMultiplier), 0, 255);
    // float b = Mathf.Clamp(parentColor.b + (parentColor.b * Random.Range(-1f, 1f) * mutationRange * colorMutationMultiplier), 0, 255);
    Color childColor = Color.HSVToRGB(h, s, v);

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

    float startingEnergy = (parent.energy/ 2) - energyPerSpawnDst * parent.stat.spawnDist;
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

      Color color = Color.HSVToRGB(Random.Range(0f,1),Random.Range(.40f,1),Random.Range(.5f, 1));
      Vector2 position = new Vector2(Random.Range(0, width), Random.Range(0, height));
      SpawnCreature(stat, position, originalCreatureStartingEnergy, color);
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.DrawWireCube(new Vector2(width /2, height / 2), new Vector2(width, height  ) );
  }
}
