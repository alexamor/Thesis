using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;

public class DemoReader : MonoBehaviour {

    public static int hopRange = 10;
    public static int dataSize = 21;

    public static float[] curVals = new float[dataSize];
    private static float[,] prevVals = new float[hopRange, dataSize];

    static string path = "Assets/LSTMOuts/aHand.txt";
    static StreamReader reader;
    static StreamWriter writer; //backup of the online reproduction

    public static void Init(bool online)
    {
        if (!online)
            reader = new StreamReader(path);
        else
            writer = new StreamWriter(path);
    }

    public static bool ReadNext()
    {
        //Read "-" line and check if there is still something to read
        if (reader.ReadLine() == null)
            return false;

        for (int i = 0; i < curVals.Length; i++)
            curVals[i] = float.Parse(reader.ReadLine(), CultureInfo.InvariantCulture);

        return true;
    }

    public static void End(bool online)
    {
        if (!online)
            reader.Close();
        else
            writer.Close();
    }

    public static Vector3 GetPos()
    {
        Vector3 aux = new Vector3(curVals[13], curVals[14], curVals[15]);

        return aux;
    }

    public static Vector4 GetRot()
    {
        Vector4 aux = new Vector4(curVals[16], curVals[17], curVals[18], curVals[19]);

        return aux;
    }

    public static void UpdateVals(float[] newVals)
    {
        writer.WriteLine("-");
        
        for(int i = 0; i < dataSize - 1; i++)
        {
            curVals[i] = newVals[i];
            writer.WriteLine(curVals[i]);
        }
    }

    public static void WriteState()
    {
        StreamWriter sWriter = new StreamWriter(@"D:\Outros\_Tese\Tensorflow\manipS.txt");

        for(int i = 0; i<hopRange;i++)
        {
            sWriter.WriteLine("-");

            for (int j = 0; j < dataSize; j++)
                sWriter.WriteLine(prevVals[i,j]);
        }


        sWriter.Close();
    }

    public static void ChangeFile()
    {
        End(false);

        reader = new StreamReader(@"D:\Outros\_Tese\Tensorflow\mHand.txt");
    }

    public static bool SearchForNewFile()
    {
        if (File.Exists(@"D:\Outros\_Tese\Tensorflow\mHand.txt"))
        {
            ChangeFile();

            return true;
        }
        else
            return false;
    }

}
