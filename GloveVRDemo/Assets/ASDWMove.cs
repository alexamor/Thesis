using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASDWMove : MonoBehaviour {

	public float speed = 10f;

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKey(KeyCode.W))
		{
			this.transform.Translate(Vector3.forward * Time.deltaTime * speed);
		}

		if (Input.GetKey(KeyCode.S))
		{
			this.transform.Translate(Vector3.back * Time.deltaTime * speed);
		}

		if (Input.GetKey(KeyCode.A))
		{
			this.transform.Translate(Vector3.left * Time.deltaTime * speed);
		}

		if (Input.GetKey(KeyCode.D))
		{
			this.transform.Translate(Vector3.right * Time.deltaTime * speed);
		}

		if (Input.GetKey(KeyCode.Q))
		{
			this.transform.Translate(Vector3.up * Time.deltaTime * speed);
		}

		if (Input.GetKey(KeyCode.E))
		{
			this.transform.Translate(Vector3.down * Time.deltaTime * speed);
		}
	}
}
