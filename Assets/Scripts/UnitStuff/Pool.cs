using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool : MonoBehaviour
{
	public static Pool ins;

	public int psize;
	public GameObject bulletPrefab;
	public Bullet[] bpool;
	int bcham;

	private void Awake()
	{
		ins = this;
		bpool = new Bullet[psize];
		for(int i = 0; i < psize; i++) {
			bpool[i] = Instantiate(bulletPrefab, transform).GetComponent<Bullet>();
		}
	}

	public Bullet GetBullet() {
		if (bcham >= psize - 2) bcham = -1;
		bcham++;
		return bpool[bcham];
    }
}
