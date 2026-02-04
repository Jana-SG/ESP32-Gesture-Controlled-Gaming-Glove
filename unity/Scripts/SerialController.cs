using UnityEngine;
using System.IO;

public class SerialController : MonoBehaviour
{
    public GameObject player; // Assign this in the Inspector

    void Update()
    {
        string path = "serial_output.txt";  // Put file in Unity project root or StreamingAssets

        if (File.Exists(path))
        {
            try
            {
                string data = File.ReadAllText(path);
                string[] parts = data.Split(' ');

                foreach (string part in parts)
                {
                    if (part.StartsWith("GyroY:"))
                    {
                        float gy = float.Parse(part.Split(':')[1]);
                        player.transform.Rotate(0, gy * Time.deltaTime, 0);
                    }
                    else if (part.StartsWith("Flex:"))
                    {
                        int flex = int.Parse(part.Split(':')[1]);
                        if (flex == 1)
                        {
                            // Insert action here, like shooting
                            Debug.Log("FIRE!");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error reading serial_output.txt: " + e.Message);
            }
        }
    }
}
