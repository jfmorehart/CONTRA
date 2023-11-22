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

	public int esize;
	public GameObject exploPrefab;
	public Boom[] epool;
	int echam;

	public int msize;
	public GameObject mPrefab;
	public Missile[] mpool;
	int mcham;

	private void Awake()
	{
		ins = this;
		bpool = new Bullet[psize];
		for(int i = 0; i < psize; i++) {
			bpool[i] = Instantiate(bulletPrefab, transform).GetComponent<Bullet>();
		}
		epool = new Boom[esize];
		for (int i = 0; i < esize; i++)
		{
			epool[i] = Instantiate(exploPrefab, transform).transform.GetChild(0).GetComponent<Boom>();
		}
		mpool = new Missile[msize];
		for (int i = 0; i < msize; i++)
		{
			mpool[i] = Instantiate(mPrefab, transform).transform.GetComponent<Missile>();
		}
	}

	public Bullet GetBullet() {
		if (bcham >= psize - 2) bcham = -1;
		bcham++;
		return bpool[bcham];
    }
	public Boom Explode()
	{
		if (echam >= esize - 2) echam = -1;
		echam++;
		return epool[echam];
	}
	public Missile GetMissile()
	{
		if (mcham >= msize - 2) mcham = -1;
		mcham++;
		return mpool[mcham];
	}
}
