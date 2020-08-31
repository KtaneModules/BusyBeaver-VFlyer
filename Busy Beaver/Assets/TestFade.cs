using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;

public class TestFade : MonoBehaviour {

	public MeshRenderer currentRenderer;

	// Use this for initialization
	void Start () {
		StartCoroutine(HandleFadeAnim());
	}

	IEnumerator HandleFadeAnim()
	{
		for (float x = 0; x < 1; x += Time.deltaTime)
        {
			if (currentRenderer.material.HasProperty("_Blend"))
				currentRenderer.material.SetFloat("_Blend", x);
			yield return new WaitForSeconds(Time.deltaTime);
        }
    }

	// Update is called once per frame
	void Update () {

	}
}
