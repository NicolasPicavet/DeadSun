﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {
    public Sprite dmgSprite;
    public AudioClip chopSound1;
    public AudioClip chopSound2;
    public SpriteRenderer spriteRenderer;
    
    private int hp = 2;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void DamageWall (int loss) {
        SoundManager.instance.RandomizeSfx(chopSound1, chopSound2);
        spriteRenderer.sprite = dmgSprite;
        hp -= loss;
        if (hp <= 0)
            gameObject.SetActive(false);
    }
}
