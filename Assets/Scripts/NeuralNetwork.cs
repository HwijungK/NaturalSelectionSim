using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;


public class NNet
{
  static int BASE_INPUT = 1;
  static int INPUT_PER_OTHER = 6;

  private int maxDetectCount;
  private int hiddenLayerCount; // number of hidden layers
  private int[] hiddenLayerNodeCount; // number of nodes in each hidden layer
  private float[][,] weights; 
  private float[][] biases;
  
  public NNet(int maxDetectCount, int[] hiddenLayerNodeCount)
  {
    this.maxDetectCount = maxDetectCount;
    this.hiddenLayerCount = hiddenLayerNodeCount.Length;
    this.weights = new float[hiddenLayerCount + 1][,];
    this.biases = new float[hiddenLayerCount + 1][];
    this.hiddenLayerNodeCount = hiddenLayerNodeCount;

    for (int i = 0; i < hiddenLayerCount + 1; i++)
    {
      weights[i] = new float[i < hiddenLayerCount ? hiddenLayerNodeCount[i] : 2 , i == 0 ? BASE_INPUT + INPUT_PER_OTHER * maxDetectCount : hiddenLayerNodeCount[i - 1]];
      biases[i] = new float[i < hiddenLayerCount ? hiddenLayerNodeCount[i] : 2];

      // Initialize With random values where weight is in [-1, 1] and bias is int [-10, 10]
      for (int r = 0; r < weights[i].GetLength(0); r++)
      {
        for (int c = 0; c < weights[i].GetLength(1); c++)
        {
          weights[i][r,c] = UnityEngine.Random.Range(-1f, 1f);
        }
        biases[i][r] = UnityEngine.Random.Range(-0.1f, 0.1f);
      }
    }
  }

  public NNet(NNet from, float mutationPercent)
  {
    this.maxDetectCount = from.maxDetectCount;
    this.hiddenLayerCount = from.hiddenLayerCount;
    this.weights = new float[hiddenLayerCount + 1][,];
    this.biases = new float[hiddenLayerCount + 1][];
    this.hiddenLayerNodeCount = from.hiddenLayerNodeCount;

    for (int i = 0; i < hiddenLayerCount + 1; i++)
    {
      weights[i] = new float[i < hiddenLayerCount ? hiddenLayerNodeCount[i] : 2 , i == 0 ? BASE_INPUT + INPUT_PER_OTHER * maxDetectCount : hiddenLayerNodeCount[i - 1]];
      biases[i] = new float[i < hiddenLayerCount ? hiddenLayerNodeCount[i] : 2];

      // Initialize With random values where weight is in [-1, 1] and bias is int [-10, 10]
      for (int r = 0; r < weights[i].GetLength(0); r++)
      {
        for (int c = 0; c < weights[i].GetLength(1); c++)
        {
          weights[i][r,c] = Mathf.Clamp(from.weights[i][r,c] + UnityEngine.Random.Range(-mutationPercent, mutationPercent), -1f, 1f);
        }
        biases[i][r] = Mathf.Clamp(from.biases[i][r] + UnityEngine.Random.Range(-mutationPercent, mutationPercent), -1f, 1f);
      }
    }
  }

  /// <summary>
  /// Calculates the response of this creature given its environment
  /// </summary>
  /// <param name="self"></param>
  /// <param name="others"></param>
  /// <returns> A tuple with values in [0, 1] and [0, 2 PI] corresponding to percent of max speed and direction</returns>
  public (Vector2 direction, float speed) Predict(Creature self, List<Creature> others)
  {
    float[] inputLayer = FormInput(self, others);

    float[] currLayer = inputLayer;
    for (int i = 0; i < hiddenLayerCount + 1; i++)
    {
      currLayer = AddVector(MatrixMultiply(weights[i], currLayer), biases[i])
        .Select(n => Sigmoid(n)).ToArray();
    }
    float angle = Mathf.Lerp(0, 2 * Mathf.PI, currLayer[0]);
    Vector2 dir = new Vector2 (Mathf.Cos(angle), Mathf.Sin(angle));
    return (dir, Mathf.Max(0.1f, currLayer[1]));
  }
  /// <summary>
  /// Given this creature and its environment return a list of input values in [0, 1]
  /// </summary>
  /// <param name="self"></param>
  /// <param name="others"></param>
  /// <returns>
  /// 0th value weigh's self's energy
  /// each subsequence 6 values measuse Color, other's movement direction, size, speed, direction to other, distance from other,    for each other in the environment capped by `maxDetectCount`;
  /// </returns>
  /// <exception cref="Exception"></exception>
  private float[] FormInput(Creature self, List<Creature> others)
  {
    float[] ret = new float[BASE_INPUT + INPUT_PER_OTHER * this.maxDetectCount];

    // Populate Base_inputs
    ret[0] = self.energy / self.stat.splitThresh;

    System.Random rnd = new System.Random();

    var sample = others.OrderBy(x => rnd.Next()).Take(this.maxDetectCount);

    int i = 0;
    foreach (Creature other in sample)
    {
      Color.RGBToHSV(other.GetComponent<SpriteRenderer>().color, out float hue, out float s, out float v);
      // Color, other's movement direction, size, speed, direction to other, distance from other
      ret[i * INPUT_PER_OTHER + BASE_INPUT] = hue; // Color
      ret[i*INPUT_PER_OTHER + BASE_INPUT + 1] = (Vector2.Dot(other.GetComponent<Rigidbody2D>().linearVelocity.normalized, ((Vector2) (self.transform.position - other.transform.position)).normalized) / 2) + 0.5f; // Whether the other creature is moving towards self 
      ret[i * INPUT_PER_OTHER + BASE_INPUT + 2] = 1 - Mathf.Pow(.5f, (other.stat.size / self.stat.size)); // Size difference from 0 to 1 with 0.5 meaning they are the same size
      ret[i * INPUT_PER_OTHER + BASE_INPUT + 3] = 1 - Mathf.Pow(.5f, (other.stat.speed / self.stat.speed)); // Speed different between 0 to 1 with 0.5 meaning they are the same speed
      ret[i * INPUT_PER_OTHER + BASE_INPUT + 4] = Vector2.SignedAngle(Vector2.down, other.transform.position - self.transform.position) / 360 + .5f; // angle towards other, with 0 being up, going clockwise
      ret[i * INPUT_PER_OTHER + BASE_INPUT + 5] = 1 - Mathf.Min(0, ((Vector2) (self.transform.position - other.transform.position)).magnitude / self.stat.detectRange); // with 0 being at detect range increasing as distance shrinks

      if (ret[(i * INPUT_PER_OTHER + BASE_INPUT)..(i * INPUT_PER_OTHER + BASE_INPUT + 6)].Any(f => f < 0 || f > 1))
      {
        Debug.Log(string.Join(", ", ret[(i * INPUT_PER_OTHER + BASE_INPUT)..(i * INPUT_PER_OTHER + BASE_INPUT + 6)]));
        throw new Exception("Input to NNet must be in [0, 1]");
      }
      i++;
    }

    return ret;
  }

  private float[] MatrixMultiply(float[,] mat, float[] vec)
  {
    float[] ret = new float[mat.GetLength(0)];

    for (int rowI = 0; rowI < ret.Length; rowI++)
    {
      float s = 0;
      for (int colI = 0; colI < mat.GetLength(1); colI++)
      {
        s += mat[rowI, colI] * vec[colI];
      }
      ret[rowI] = s;
    }
    return ret;
  }
  private float[] AddVector(float[] a, float[] b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Cannot add vectors of differing lengths");
    }
    float[] r = new float[a.Length];
    for (int i = 0; i < a.Length; i++)
    {
      r[i] = a[i] + b[i];
    }
    return r;
    
  }
  private float Sigmoid(float a)
  {
    return 1 / (1 + Mathf.Pow(2.71f, -a));
  }
}