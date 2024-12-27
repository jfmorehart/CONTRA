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

	public int atamsize;
	public GameObject atamPrefab;
	public ATAM[] atampool;
	int atamcham;

	public int rsize;
	public GameObject rPrefab;
	public AppearEffect[] rpool;
	int rcham;

	private void Start()
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

		rpool = new AppearEffect[rsize];
		for (int i = 0; i < rsize; i++)
		{
			rpool[i] = Instantiate(rPrefab, transform).transform.GetComponent<AppearEffect>();
		}

		if (Map.multi) return; //we're not pooling them in multiplayer
		atampool = new ATAM[atamsize];
		for (int i = 0; i < atamsize; i++)
		{
			atampool[i] = Instantiate(atamPrefab, transform).transform.GetComponent<ATAM>();
		}
	}
	private void Update()
	{
		for(int i = 0; i < psize; i++) {
			bpool[i].FUpdate();
		}
		for (int i = 0; i < msize; i++)
		{
			mpool[i].FUpdate();
		}
		for (int i = 0; i < esize; i++)
		{
			epool[i].FUpdate();
		}
		for (int i = 0; i < rsize; i++)
		{
			rpool[i].FUpdate();
		}
		//if (Map.multi) return; //we're not pooling them in multiplayer
		//for (int i = 0; i < atamsize; i++)
		//{
		//	atampool[i].FUpdate();
		//}
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
	public ATAM GetATAM()
	{
		if (atamcham >= atamsize - 2) atamcham = -1;
		atamcham++;
		return atampool[atamcham];
	}
	public AppearEffect GetRingEffect()
	{
		if (rcham >= rsize - 2) rcham = -1;
		rcham++;
		return rpool[rcham];
	}
}
