using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibrator : MonoBehaviour {

    //Sensor indexes
    /* 0 = ThumbPh2
    * 1 = ThumbPh1
    * 2 = IndexPh2
    * 3 = IndexPh1
    * 4 = MiddlePh2
    * 5 = MiddlePh1
    * 6 = RingPh2
    * 7 = RingPh1
    * 8 = LittlePh2
    * 9 = LittlePh1
    * 10 = PalmArch
    * 11 = 
    * 12 = 
    * 13 = 
    * 14 = 
    * 15 = 
    * 16 = 
    * 17 = 
    * 18 = 
    * 19 = AbdThumb
    * 20 = AbdIndex
    * 21 = AbdRing
    * 22 = AbdLittle
    */

    //values from which sensor starts
    public static int[] offset = { 260, 200, 400, 260, 265, 300, 280, 330, 280, 330, 300, 0, 0, 0, 0, 0, 0, 0, 0, 600, 930, 880, 770 };

    //max value returned by each sensors
    public static int[] max = { 760, 490, 770, 520, 740, 700, 780, 620, 600, 600, 810, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000, 930, 970, 920, 820 };

}
