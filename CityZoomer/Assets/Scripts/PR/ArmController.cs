using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Animator_Secondary0 = GameObject.Find("Secondary0").GetComponent<Animator>();
//if (Input.GetKeyDown(UI.cl.fire)) Animator_Secondary0.Play("Fire", -1, 0.0f);

namespace PR
{
    public class ArmController : MonoBehaviour
    {
        private GameObject GO_Weapon_9mm, GO_Weapon_Welrod, GO_Weapon_Luger;
        private GameObject[] GO_Weapons;
        private Animator Animator_Secondary0;
        private int Weapon_Current = 0;
        

        private void Awake()
        {
            GO_Weapon_9mm = GameObject.Find("Weapon_9mm");
            GO_Weapon_Welrod = GameObject.Find("Weapon_Welrod");
            GO_Weapon_Luger = GameObject.Find("Weapon_Luger");
            GO_Weapons = new GameObject[] { GO_Weapon_9mm, GO_Weapon_Welrod, GO_Weapon_Luger };
        }

        void Start()
        {
            GO_Weapon_Welrod.SetActive(false);
            GO_Weapon_Luger.SetActive(false);
        }

        
        private void Update()
        {
            if (UI.isScrollingUp) WeaponSwitch(Weapon_Current + 1);
            else if (UI.isScrollingDown) WeaponSwitch(Weapon_Current - 1);
        }
        
        private void WeaponSwitch(int index)
        {
            if (index < 0 || index >= GO_Weapons.Length || Weapon_Current == index) return;
            GO_Weapons[Weapon_Current].SetActive(false);
            GO_Weapons[index].SetActive(true);
            Weapon_Current = index;
        }
    }
}
