using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class Logger : MonoBehaviour
{
    // path from home
    public string path;
    public string fileName = "Hello";
    public bool overwriteFile = false;
    private string _fullPath;


    // Logging Info
    public float timeBetweenLogs;
    private float _nextLogTime;

    private List<Creature> _creatures;
    private void Start()
    {
        _creatures = GameManager.instance.creatures;

        // Set a variable to the Documents path.
        string docPath =
          Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _fullPath = Path.Combine(docPath, path, fileName);
        
        Debug.Log("File Exists: " + File.Exists(_fullPath));

        // Check if file already exists
        if (File.Exists(_fullPath) && !overwriteFile)
        {
            Debug.LogWarning("The File your trying to write to already exists");
        }
        else
        {
            Debug.Log("Writing To File: " + _fullPath);
            using (StreamWriter outputFile = new StreamWriter(_fullPath))
            {
                outputFile.WriteLine("time, x,y,speed, detectRange, size, splitThresh, spawnDist, speedCoef, speedConst, sizeCoef, sizeConst, autoGen, energyConsumption, color");
            }
        }
    }

    private void Update()
    {
        if (Time.time > _nextLogTime)
        {
            LogToFile();
            _nextLogTime += timeBetweenLogs;
        }
    }

    private void LogToFile()
    {
        using (StreamWriter outputFile = new StreamWriter(_fullPath, true))
        {
            // outputFile.Write(String.Join(',', new String[] {"" +_nextLogTime.ToString("F2"), "" + _creatures.Count}) + '\n');
            foreach (Creature c in GameManager.instance.creatures)
            {
                outputFile.WriteLine(_nextLogTime.ToString("F1") + "," + c.transform.position.x + "," + c.transform.position.y + "," + c.stat.ToString() + "," + c.energyGenRate + "," + c.energyConsumptionRate + "," + c.color);
            }
        }
    }
}
