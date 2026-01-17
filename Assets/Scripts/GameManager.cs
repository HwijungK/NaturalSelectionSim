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

  // Logger Information
  [HideInInspector]
  public List<Creature> creatures;
    
    

  private void Awake()
  {
    if (instance != null) Destroy(this);
    else instance = this;
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
}
