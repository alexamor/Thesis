using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticICub : MonoBehaviour {

	public int finger = 0;
	public iCubController glove;
	//public static float gama = 2f;
	//public static float miu = 0.5f;

	private GameObject grabbedObject;

	/*
	private Vector3[] conPoints = new Vector3[4];
	private float contSpread = 0.005f;
	private Vector3[] forces = new Vector3[4];
	private Vector3[] tanContacts = new Vector3[4];
	private Vector3[] tanForces = new Vector3[4];
	private Vector3[] normals = new Vector3[4];
	private Vector3[] finalforces = new Vector3[4];
	*/
	// Use this for initialization
	void Start()
	{
		/*
		for(int i = 0; i < conPoints.Length; i++)
		{
			forces[i] = new Vector3();
			tanContacts[i] = new Vector3();
			tanForces[i] = new Vector3();
			finalforces[i] = new Vector3();
		}

		ResetContactPoints();
		*/
	}

	// Update is called once per frame
	void Update()
	{

		/*
		for(int i = 0; i < forces.Length; i++)
		{
			//If there is a contact point
			if (conPoints[i].x != -999f)
			{				
				forces[i] = gama * (conPoints[i] - this.transform.position);
				Debug.Log("FORCE: " + forces[i].x + "  ct:" + conPoints[0].x + "  this:" + this.transform.position.x);
				tanContacts[i] = Vector3.Dot(forces[i], normals[i]) * normals[i];
				tanForces[i] = forces[i] - tanContacts[i];

				//Check if force is inside Coulomb cone
				if(true)//Vector3.Dot(forces[i], normals[i]) > 0 && tanForces[i].magnitude <= miu * Vector3.Dot(forces[i], normals[i]))
				{
					finalforces[i] = tanForces[i];

					Debug.Log("Fixed");
				}
				else //The finger is sliding
				{
					finalforces[i] = miu * tanForces[i];

					Debug.Log("Sliding");

					//Recalculate contact points
					ResetContactPoints();
					RaycastHit hit;
					Physics.Raycast(this.transform.position, conPoints[0] - this.transform.position, out hit, 0.04f);
					conPoints[0] = hit.point;
					normals[0] = hit.normal;

					GetContactPoints();
				}

				//Apply forces to object
				grabbedObject.GetComponent<Rigidbody>().AddForceAtPosition(tanContacts[i], conPoints[i]);
				grabbedObject.GetComponent<Rigidbody>().AddForceAtPosition(finalforces[i], conPoints[i]);

				Debug.DrawRay(conPoints[i], tanContacts[i], Color.red);
				Debug.DrawRay(conPoints[i], finalforces[i], Color.red);
			}	

				
		}

		Debug.Log(this.gameObject.name + "-force " + forces[0].x + "  normal: " + normals[0].x + "  tanC: " + tanContacts[0].x + "  tanF: " + tanForces[0].x);
		*/
	}

	/*

	void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.tag == "Object")
		{
			glove.UpdateVibro(0, finger, 90);
			//////Depois para objetos mais elaborados é possivel que isto precise de guardar os varios colliders por causa do OnTriggerExit()
			grabbedObject = collision.gameObject;

			this.GetComponent<Collider>().isTrigger = true;

			glove.AddnRemoveContact(true, grabbedObject);
			/*
			//Get contact points
			conPoints[0] = collision.contacts[0].point;

			RaycastHit hit;
			Physics.Raycast(this.transform.position, conPoints[0] - this.transform.position, out hit, 0.04f);
			normals[0] = hit.normal;

			GetContactPoints();
			*/
	//Debug.Log(this.gameObject.name + "-ColEnter " + collision.gameObject.name + "  ctct: " + conPoints[0] + "  normal: " + normals[0]);

	/*
	Debug.Log("Collision Enter " + this.gameObject.name);
}

}

void OnCollisionExit(Collision collision)
{
if (collision.collider.gameObject.tag == "Object")
{
	this.GetComponent<Collider>().isTrigger = false;
	glove.UpdateVibro(0, finger, 0);
	glove.AddnRemoveContact(false, null);
	//ResetContactPoints();

	Debug.Log("Collision Exit " + this.gameObject.name);
}
}

*/
	void OnTriggerEnter(Collider col)
	{
		if (col.tag == "Object")
		{
			glove.UpdateVibro(0, finger, 90);
			//////Depois para objetos mais elaborados é possivel que isto precise de guardar os varios colliders por causa do OnTriggerExit()
			grabbedObject = col.gameObject;

			//this.GetComponent<Collider>().isTrigger = true;

			glove.AddnRemoveContact(true, grabbedObject);
			/*
			//Get contact points
			conPoints[0] = collision.contacts[0].point;

			RaycastHit hit;
			Physics.Raycast(this.transform.position, conPoints[0] - this.transform.position, out hit, 0.04f);
			normals[0] = hit.normal;

			GetContactPoints();
			*/
			//Debug.Log(this.gameObject.name + "-ColEnter " + collision.gameObject.name + "  ctct: " + conPoints[0] + "  normal: " + normals[0]);


			Debug.Log("Collision Enter " + this.gameObject.name);
		}
	}

	void OnTriggerExit(Collider col)
	{
		if (col.gameObject.tag == "Object")
		{
			//this.GetComponent<Collider>().isTrigger = false;
			glove.UpdateVibro(0, finger, 0);
			glove.AddnRemoveContact(false, null);
			//ResetContactPoints();

			Debug.Log("Collision Exit " + this.gameObject.name);
		}
	}


	/*
	void OnTriggerExit(Collider collision)
	{
		if (collision.gameObject.tag == "Object")
		{
			this.GetComponent<Collider>().isTrigger = false;
			glove.UpdateVibro(0, finger, 0);
			ResetContactPoints();

			Debug.Log("Trigger Exit " + collision.gameObject.name);
		}
	}
	*/
	/*
	void GetContactPoints()
	{
		for (int i = 1; i < conPoints.Length; i++)
		{
			//Draw a ray in the finger to object direction but with a shift int the position
			RaycastHit hit;
			Vector3 auxVec = new Vector3(conPoints[0].y, -conPoints[0].x, 0f).normalized;
			if (Physics.Raycast(conPoints[0] + auxVec * contSpread, conPoints[0] - this.transform.position, out hit, 0.04f))
			{
				conPoints[i] = hit.point;

				normals[i] = hit.normal;

				if (i == 1)
					auxVec = new Vector3(conPoints[0].z, 0f, -conPoints[0].x);
				else if (i == 2)
					auxVec = new Vector3(0f, conPoints[0].z, -conPoints[0].y);
			}
		}
	}

	void ResetContactPoints()
	{
		for(int i = 0; i < conPoints.Length; i++)
			conPoints[i] = new Vector3(-999f, -999f, -999f);
	}
	*/
}
