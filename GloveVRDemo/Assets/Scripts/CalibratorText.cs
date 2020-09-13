using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CalibratorText : MonoBehaviour {

    public int COMPORT_RightGlove = 10;
    private Text fingerText;

    //communicaton between glove and unity
    VMG30_Driver gloveR = new VMG30_Driver();

    float lastPackageR;

    // Use this for initialization
    void Start()
    {
        

        Debug.Log("Start\n");
        lastPackageR = Time.fixedTime;

        gloveR.Init(COMPORT_RightGlove, Constants.RightHanded, Constants.PKG_QUAT_FINGER);
        gloveR.StartCommunication();

        fingerText = this.GetComponent<Text>();
    }

    void OnApplicationQuit()
    {
        Debug.Log("Close app\n");
        gloveR.StopCommunication();

        while (gloveR.ThreadStatus)
        {

        }
        Debug.Log("Thread right glove terminated\n");
    }

    void Update()
    {
        //check pressure value L and R
        VMGValues vr = gloveR.GetPackage();

        //GameObject game = GameObject.FindGameObjectWithTag("GameController");
        //GameController scriptg = game.GetComponent<GameController>();

        if (gloveR.Connected)
        {
            fingerText.text = "Thumb:: Ph2: " + vr.SensorValues[0] + " Ph1: " + vr.SensorValues[1] + " PalmArch: " + vr.SensorValues[10] + " Abs: " + vr.SensorValues[19]
                + Environment.NewLine + "Index:: Ph2: " + vr.SensorValues[2] + " Ph1: " + vr.SensorValues[3] + " Abs: " + vr.SensorValues[20]
                + Environment.NewLine + "Middle:: Ph2: " + vr.SensorValues[4] + " Ph1: " + vr.SensorValues[5]
                + Environment.NewLine + "Ring:: Ph2: " + vr.SensorValues[6] + " Ph1: " + vr.SensorValues[7] + " Abs: " + vr.SensorValues[21]
                + Environment.NewLine + "Little:: Ph2: " + vr.SensorValues[8] + " Ph1: " + vr.SensorValues[9] + " Abs: " + vr.SensorValues[22];

        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        int i = 0;
       
        if (gloveR.Reconnect)
        {
            //Debug.Log("Restart glove R\n");
            gloveR.Reconnect = false;
            lastPackageR = Time.fixedTime;
            gloveR.StartCommunication();
        }
        //check if a new package is arrived from glove
        if (gloveR.NewPackageAvailable())
        {
            lastPackageR = Time.fixedTime;
            //Debug.Log("New package R\n");
            VMGValues v = gloveR.GetPackage();
        }
        else
        {
            float dtime = Time.fixedTime - lastPackageR;
            if (dtime > Constants.GLOVE_TIMEOUT)
            {
                //Debug.Log("Stop glove R\n");
                lastPackageR = Time.fixedTime;
                gloveR.StopCommunication();
            }
        }

    }

}
