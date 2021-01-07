using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HapticHand : MonoBehaviour {

	public int finger = 0;
	public HandController glove;
	public static float gama = 20000f;
	//public static float miu = 0.5f;

	private GameObject grabbedObject;

	private bool[] inContact = new bool[5];  
	
	private Vector3[] conPoints = new Vector3[5];
	private float contSpread = 0.005f;
	private Vector3[] forces = new Vector3[5];
	private Vector3[] tanContacts = new Vector3[5];
	private Vector3[] tanForces = new Vector3[5];
	private Vector3[] normals = new Vector3[5];
	private Vector3[] finalforces = new Vector3[5];
	
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
		*/
		System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		ResetContactPoints();
		
	}

	// Update is called once per frame
	void Update()
	{
		Vector3 force = new Vector3();
		float str = 0f; 
		
		//Only apply forces if the object is not yet grabbed
		if(grabbedObject != null && glove.sensorvalues == -1)
		{
			for(int i =0; i < inContact.Length; i++)
			{
				if(inContact[i])
				{
					Vector3 dist = new Vector3();
					
					if (grabbedObject.GetComponent<Collider>().bounds.Contains(this.transform.position))
					{
						dist = this.transform.position - grabbedObject.GetComponent<Collider>().ClosestPointOnBounds(this.transform.position);
						str = this.GetComponent<SphereCollider>().radius + dist.magnitude;
					}
					else
					{
						dist = grabbedObject.GetComponent<Collider>().ClosestPointOnBounds(this.transform.position) - this.transform.position;
						str = this.GetComponent<SphereCollider>().radius - dist.magnitude;
					}

					//force = dist.normalized * str;
					force = transform.TransformVector(new Vector3(0,-1,0)) * str;

					if(grabbedObject.GetComponent<Rigidbody>() != null)
						grabbedObject.GetComponent<Rigidbody>().AddForceAtPosition(force * gama * Time.deltaTime, grabbedObject.GetComponent<Collider>().ClosestPointOnBounds(this.transform.position));
					else
					{
						GameObject rigGO = grabbedObject.transform.parent.gameObject;

						while (rigGO.GetComponent<Rigidbody>() == null)
							rigGO = rigGO.transform.parent.gameObject;

						rigGO.GetComponent<Rigidbody>().AddForceAtPosition(force * gama * Time.deltaTime, grabbedObject.GetComponent<Collider>().ClosestPointOnBounds(this.transform.position));
					}


					//Debug.Log(Physics.ClosestPoint(this.transform.position, grabbedObject.GetComponent<Collider>(), grabbedObject.transform.position, grabbedObject.transform.rotation).x + "-" + this.transform.position.x);
					//Debug.Log(Physics.ClosestPoint(this.transform.position, grabbedObject.GetComponent<Collider>(), grabbedObject.transform.position, grabbedObject.transform.rotation).y + "-" + this.transform.position.y);
					//Debug.Log(Physics.ClosestPoint(this.transform.position, grabbedObject.GetComponent<Collider>(), grabbedObject.transform.position, grabbedObject.transform.rotation).z + "-" +this.transform.position.z);
				}
			}
		}
		
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
	void OnTriggerEnter(Collider col)
	{
		if (col.tag == "Object")
		{
			glove.UpdateVibro(0, finger, 100);
			//////Depois para objetos mais elaborados é possivel que isto precise de guardar os varios colliders por causa do OnTriggerExit()
			grabbedObject = col.gameObject;

			//Get contact points
			Vector3 conPts = col.ClosestPointOnBounds(transform.position);
			Vector3 dir = col.gameObject.transform.position - this.transform.position;//conPts - this.transform.position;

			RaycastHit hit;
			Debug.Log(Physics.Raycast(this.transform.position - (dir.normalized * 1f), dir, out hit));
			//Debug.DrawRay(this.transform.position - (dir.normalized * 1f), dir*100, Color.red, 20f, true);
			Vector3 normal = this.transform.InverseTransformVector(hit.normal);
			//Debug.Log("f:" + finger + " h:" + hit.normal + " n:" + normal);

			Vector3 cDist = this.transform.InverseTransformVector(col.gameObject.transform.position - this.transform.position);

			//If its the thumb make it value more
			if (finger == 0)
				glove.AddnRemoveContact(5, finger, grabbedObject, normal, cDist);
			else
				glove.AddnRemoveContact(1, finger, grabbedObject, normal, cDist);

			inContact[finger] = true;

			/*
			//Get contact points
			conPoints[0] = collision.contacts[0].point;

			RaycastHit hit;
			Physics.Raycast(this.transform.position, conPoints[0] - this.transform.position, out hit, 0.04f);
			normals[0] = hit.normal;

			GetContactPoints();
			*/
			//Debug.Log(this.gameObject.name + "-ColEnter " + collision.gameObject.name + "  ctct: " + conPoints[0] + "  normal: " + normals[0]);


			Debug.Log("Collision Enter " + this.gameObject.name + " " + Time.timeScale + " " + Time.time);
		}
	}

	/*
	void OnTriggerStay(Collider col)
	{
		if (col.tag == "Object")
		{
			glove.UpdateVibro(0, finger, 90);
		}
	}
	*/

	void OnTriggerExit(Collider col)
	{
		if (col.gameObject.tag == "Object")
		{
			glove.UpdateVibro(0, finger, 0);
			//If its the thumb make it value more
			if (finger == 0)
				glove.AddnRemoveContact(-5, finger, null, Vector3.zero, Vector3.zero);
			else
				glove.AddnRemoveContact(-1, finger, null, Vector3.zero, Vector3.zero);

			inContact[finger] = false;

			Debug.Log("Collision Exit " + this.gameObject.name + " " + Time.timeScale + " " + Time.time);
		}
	}



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
	*/

	void ResetContactPoints()
	{
		for(int i = 0; i < conPoints.Length; i++)
			conPoints[i] = new Vector3(-999f, -999f, -999f);
	}
	
}
