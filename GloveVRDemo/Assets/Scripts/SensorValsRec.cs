using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SensorValsRec : MonoBehaviour {

	public bool Save = false;
	public bool Load = false;
	private HandController controller;
	private int phase = 0;
	private string path = "Assets/Calib/calVals.txt";
	private StreamWriter writer;

	// Use this for initialization
	void Start () {

		controller = GetComponent<HandController>();
		
		if(Save)
		{
			writer = new StreamWriter(path);
		}

		if(Load)
		{
			StreamReader reader = new StreamReader(path);

			for(int i = 0; i < 8; i++)
			{
				Calibrator.offset[i+2] = int.Parse(reader.ReadLine());
			}

			reader.ReadLine();

			for (int i = 0; i < 8; i++)
			{
				Calibrator.max[i + 2] = int.Parse(reader.ReadLine());
			}

			reader.Close();
		}
	}
	
	// Update is called once per frame
	void Update () {
		
		if(Save)
        {
			if(phase == 0)
            {
				Debug.Log("Phase 1: Open hand (press Space to Continue)");
				phase++;
            }
			else if(phase == 1)
			{
				if(Input.GetKeyDown(KeyCode.Space))
				{
					int[] vals = new int[8];

					vals = controller.ReturnSensorVals();

					for (int i = 0; i < vals.Length; i++)
						writer.WriteLine(vals[i]);

					writer.WriteLine("-");

					phase++;
					Debug.Log("Phase 2: Closed Hand (press Space to Continue)");
				}
			}
			else if (phase == 2)
			{
				if (Input.GetKeyDown(KeyCode.Space))
				{
					int[] vals = new int[8];

					vals = controller.ReturnSensorVals();

					for (int i = 0; i < vals.Length; i++)
						writer.WriteLine(vals[i]);

					phase++;
					Debug.Log("Calibration recorded");
					writer.Close();
				}
			}
		}

	}
}
