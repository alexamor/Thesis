﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Randomizer : MonoBehaviour {

	public float xRange = 0.1f;
	public float zRange = 0.1f;
	public bool rotate = true;
	public float aRange = 20f;

	// Use this for initialization
	void Start () {

		this.transform.Translate(new Vector3(xRange * Random.Range(-1.0f, 1.0f), zRange * Random.Range(-1.0f, 1.0f), zRange * Random.Range(-1.0f, 1.0f)));
		if (rotate)
			this.transform.Rotate(new Vector3(0f, 1f, 0f), aRange * Random.Range(-1.0f, 1.0f), Space.World);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
