using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
  public static GameManager instance;
    public Creature creaturePrefab;

    [Header("global settings")]
    public float creatureDecisionRefreshTime;
    public float witdth, height;
    [Range (0.8f, 1.2f)]
    public float mutationRange;

    [Header("World Rules")]
    public float energyAutoGenRate = 1;

    // Logger Information
    [HideInInspector]
    public List<Creature> creatures;
    
    

    private void Awake()
  {
    if (instance != null) Destroy(this);
    else instance = this;
  }

    public Creature SpawnCreature(CreatureStat stat)
  {
    return null;
  }
}
